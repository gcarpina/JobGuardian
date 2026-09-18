using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
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
        ILogger<JobGuardianHostedService> logger)
    {
        Jobs = jobs;
        _scopeFactory = scopeFactory;
        _coordinator = coordinator;
        _stateManager = stateManager;
        _identityProvider = identityProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "JobGuardian hosted service started");

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

            try
            {
                await _coordinator.ExecuteAsync(
                    CreateExecution(descriptor),
                    CreateOptions(descriptor),
                    job.ExecuteAsync,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Job {JobKey} execution failed",
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