using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Abstractions;
using JobGuardian.Core.Execution;
using JobGuardian.Core.Models;
using JobGuardian.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using JobGuardian.Core.State;

namespace JobGuardian.Core.Hosting;

internal sealed class JobGuardianHostedService
    : BackgroundService
{
    private readonly IJobExecutionCoordinator _coordinator;

    private readonly IJobStateManager _stateManager;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly RuntimeOptions _options;

    private readonly IExecutionIdentityProvider _identityProvider;

    private readonly IExecutionHistoryStore? _executionHistoryStore;

    private readonly IHostEnvironment? _hostEnvironment;

    public IEnumerable<JobDescriptor> Jobs
    {
        get;
    }

    private readonly ILogger<JobGuardianHostedService>
        _logger;

    public JobGuardianHostedService(
        IEnumerable<JobDescriptor> jobs,
        IServiceScopeFactory scopeFactory,
        IJobExecutionCoordinator coordinator,
        IJobStateManager stateManager,
        IExecutionIdentityProvider identityProvider,
        RuntimeOptions options,
        ILogger<JobGuardianHostedService> logger,
        IExecutionHistoryStore? executionHistoryStore = null,
        IHostEnvironment? hostEnvironment = null)
    {
        Jobs = jobs;
        _scopeFactory = scopeFactory;
        _coordinator = coordinator;
        _stateManager = stateManager;
        _identityProvider = identityProvider;
        _options = options;
        _logger = logger;
        _executionHistoryStore = executionHistoryStore;
        _hostEnvironment = hostEnvironment;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "JobGuardian hosted service started");

        if (_executionHistoryStore is null)
        {
            _logger.LogWarning(
                "Execution history is disabled because no {ExecutionHistoryStore} is registered",
                nameof(IExecutionHistoryStore));
        }
        else if (_hostEnvironment is null)
        {
            _logger.LogWarning(
                "Host environment is unavailable; execution history will use fallback application metadata");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecuteJobsAsync(
                stoppingToken);

            await Task.Delay(
                _options.PollingInterval,
                stoppingToken);
        }
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "JobGuardian hosted service stopping");

        await base.StopAsync(
            cancellationToken);

        _logger.LogInformation(
            "JobGuardian hosted service stopped");
    }

    internal void DiscoverJobs()
    {
        foreach (var _ in Jobs)
        {
            _scopeFactory.CreateScope();
        }
    }

    internal void ResolveJobs()
    {
        foreach (var descriptor in Jobs)
        {
            using var scope =
                _scopeFactory.CreateScope();

            scope.ServiceProvider
                .GetService(
                    descriptor.JobType);
        }
    }

    internal async Task ExecuteJobsAsync(
        CancellationToken cancellationToken)
    {
        foreach (var descriptor in Jobs)
        {
            if (!await IsExecutionAllowedAsync(
                    descriptor,
                    cancellationToken))
            {
                continue;
            }

            using var scope =
                _scopeFactory.CreateScope();

            var job =
                (IJob?)scope.ServiceProvider
                    .GetService(
                        descriptor.JobType);

            if (job is null)
            {
                continue;
            }

            var execution =
                CreateExecution(descriptor);

            var historyEntry =
                CreateHistoryEntry(
                    descriptor,
                    execution,
                    DateTimeOffset.UtcNow);

            var historyCreated =
                await TryCreateHistoryAsync(
                    historyEntry,
                    cancellationToken);

            try
            {
                var executionResult =
                    await _coordinator.ExecuteAsync(
                        execution,
                        CreateOptions(descriptor),
                        job.ExecuteAsync,
                        cancellationToken);

                await TryCompleteHistoryAsync(
                    historyEntry,
                    historyCreated,
                    executionResult.Outcome,
                    GetFailureCategory(
                        executionResult.Outcome));

                try
                {
                    await _stateManager.HandleExecutionResultAsync(
                        descriptor.JobKey,
                        descriptor.Policy.FailurePolicy,
                        executionResult,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to update state for job {JobKey} after outcome {Outcome}",
                        descriptor.JobKey,
                        executionResult.Outcome);
                }

                switch (executionResult.Outcome)
                {
                    case ExecutionOutcome.Failed:
                        _logger.LogError(
                            executionResult.Exception,
                            "Job {JobKey} execution failed",
                            descriptor.JobKey);
                        break;

                    case ExecutionOutcome.LeaseLost:
                        _logger.LogWarning(
                            executionResult.Exception,
                            "Job {JobKey} execution stopped after lease ownership was lost",
                            descriptor.JobKey);
                        break;

                    case ExecutionOutcome.Cancelled:
                        _logger.LogInformation(
                            "Job {JobKey} execution was cancelled",
                            descriptor.JobKey);
                        break;
                }
            }
            catch (Exception ex)
            {
                await TryCompleteHistoryAsync(
                    historyEntry,
                    historyCreated,
                    ExecutionOutcome.Failed,
                    "InfrastructureFailure");

                _logger.LogError(
                    ex,
                    "Job {JobKey} execution coordination failed",
                    descriptor.JobKey);
            }
        }
    }

    private ActiveExecution CreateExecution(
        JobDescriptor descriptor)
    {
        return new ActiveExecution
        {
            JobKey = descriptor.JobKey,
            ExecutionId = Guid.NewGuid(),
            OwnerId = _identityProvider.GetOwnerId()
        };
    }

    private static JobExecutionOptions CreateOptions(
        JobDescriptor descriptor)
    {
        return new JobExecutionOptions
        {
            LeaseDuration =
                descriptor.Policy.LeaseDuration,

            HeartbeatInterval =
                descriptor.Policy.HeartbeatInterval
        };
    }

    private ExecutionHistoryEntry CreateHistoryEntry(
        JobDescriptor descriptor,
        ActiveExecution execution,
        DateTimeOffset startedAtUtc)
    {
        return new ExecutionHistoryEntry
        {
            ExecutionId = execution.ExecutionId,
            JobKey = descriptor.JobKey,
            ProtocolVersion = ProtocolVersions.ProtocolVersion,
            SchemaVersion = ProtocolVersions.SchemaVersion,
            ApplicationName =
                GetApplicationName(),
            ApplicationVersion =
                System.Reflection.Assembly
                    .GetEntryAssembly()
                    ?.GetName()
                    .Version
                    ?.ToString(),
            Environment =
                GetEnvironmentName(),
            OwnerId = execution.OwnerId,
            StartedAtUtc = startedAtUtc,
            RunType = RunType.Scheduled,
            TriggeredBy = TriggeredBy.Scheduler
        };
    }

    private string GetApplicationName()
    {
        if (!string.IsNullOrWhiteSpace(
            _hostEnvironment?.ApplicationName))
        {
            return _hostEnvironment.ApplicationName;
        }

        return System.Reflection.Assembly
            .GetEntryAssembly()
            ?.GetName()
            .Name
            ?? AppDomain.CurrentDomain.FriendlyName;
    }

    private string GetEnvironmentName()
    {
        if (!string.IsNullOrWhiteSpace(
            _hostEnvironment?.EnvironmentName))
        {
            return _hostEnvironment.EnvironmentName;
        }

        return Environment.GetEnvironmentVariable(
                "DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable(
                "ASPNETCORE_ENVIRONMENT")
            ?? "Unknown";
    }

    private async Task<bool> TryCreateHistoryAsync(
        ExecutionHistoryEntry entry,
        CancellationToken cancellationToken)
    {
        if (_executionHistoryStore is null)
        {
            return false;
        }

        try
        {
            await _executionHistoryStore.CreateAsync(
                entry,
                cancellationToken);

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to create execution history for {ExecutionId}",
                entry.ExecutionId);

            return false;
        }
    }

    private async Task TryCompleteHistoryAsync(
        ExecutionHistoryEntry entry,
        bool historyCreated,
        ExecutionOutcome outcome,
        string? failureCategory)
    {
        if (_executionHistoryStore is null || !historyCreated)
        {
            return;
        }

        try
        {
            await _executionHistoryStore.UpdateAsync(
                entry with
                {
                    EndedAtUtc = DateTimeOffset.UtcNow,
                    Outcome = outcome,
                    FailureCategory = failureCategory
                },
                CancellationToken.None);
        }
        catch (Exception historyException)
        {
            _logger.LogError(
                historyException,
                "Failed to complete execution history for {ExecutionId} with outcome {Outcome}",
                entry.ExecutionId,
                outcome);
        }
    }

    private static string? GetFailureCategory(
        ExecutionOutcome outcome)
    {
        return outcome switch
        {
            ExecutionOutcome.Failed => "ApplicationError",
            ExecutionOutcome.LeaseLost => "LeaseLost",
            ExecutionOutcome.Cancelled => "Cancelled",
            _ => null
        };
    }

    private async Task<bool> IsExecutionAllowedAsync(
        JobDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        var state =
            await _stateManager.GetCurrentStateAsync(
                descriptor.JobKey,
                cancellationToken);

        if (state == JobState.Blocked)
        {
            _logger.LogInformation(
                "Job {JobKey} execution skipped because the job is blocked",
                descriptor.JobKey);

            return false;
        }

        return true;
    }
}