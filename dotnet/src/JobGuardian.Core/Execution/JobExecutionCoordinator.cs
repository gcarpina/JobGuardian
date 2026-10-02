using System.Diagnostics;
using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Contracts;
using JobGuardian.Core.Models;
using JobGuardian.Core.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobGuardian.Core.Execution;

/// <summary>
/// Executes a job while coordinating lease acquisition, heartbeat renewal, and cleanup.
/// </summary>
public sealed class JobExecutionCoordinator
    : IJobExecutionCoordinator
{
    private readonly ILeaseStore _leaseStore;

    private readonly ILeaseHeartbeatService _heartbeatService;

    private readonly ILogger<JobExecutionCoordinator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobExecutionCoordinator"/> class.
    /// </summary>
    /// <param name="leaseStore">
    /// The lease store used to acquire and release execution ownership.
    /// </param>
    /// <param name="heartbeatService">
    /// The heartbeat service used to renew the lease while the job runs.
    /// </param>
    /// <param name="logger">
    /// The logger used to report callback retries, when logging is configured.
    /// </param>
    public JobExecutionCoordinator(
        ILeaseStore leaseStore,
        ILeaseHeartbeatService heartbeatService,
        ILogger<JobExecutionCoordinator>? logger = null)
    {
        _leaseStore = leaseStore;
        _heartbeatService = heartbeatService;
        _logger =
            logger
            ?? NullLogger<JobExecutionCoordinator>.Instance;
    }

    /// <summary>
    /// Executes the supplied job function while holding a valid lease.
    /// </summary>
    /// <param name="execution">
    /// The execution identity and owner metadata for the run.
    /// </param>
    /// <param name="options">
    /// The lease and heartbeat options for the current execution.
    /// </param>
    /// <param name="job">
    /// The callback that performs the actual job work.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the execution.
    /// </param>
    /// <returns>
    /// The outcome of the execution attempt.
    /// </returns>
    public async Task<ExecutionResult> ExecuteAsync(
        ActiveExecution execution,
        JobExecutionOptions options,
        Func<CancellationToken, Task> job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(job);

        JobExecutionRetrySettings.Validate(
            options.MaxAttempts,
            options.RetryDelay);

        var startedAt =
            Stopwatch.GetTimestamp();

        JobGuardianTelemetry.ExecutionStarted();

        using var activity =
            JobGuardianTelemetry.StartActivity(
                "JobGuardian.ExecutionAttempt",
                execution);
        activity?.SetTag(
            "jobguardian.execution.max_attempts",
            options.MaxAttempts);

        ExecutionResult? result = null;

        try
        {
            result =
                await ExecuteCoreAsync(
                    execution,
                    options,
                    job,
                    cancellationToken);

            activity?.SetTag(
                "jobguardian.outcome",
                result.Outcome.ToString());

            if (result.Outcome is ExecutionOutcome.Failed
                or ExecutionOutcome.LeaseLost)
            {
                activity?.SetTag(
                    "error.type",
                    result.Exception?.GetType().FullName);
                activity?.SetStatus(
                    ActivityStatusCode.Error);
            }

            return result;
        }
        catch (Exception exception)
        {
            activity?.SetTag(
                "error.type",
                exception.GetType().FullName);
            activity?.SetStatus(
                ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            JobGuardianTelemetry.ExecutionCompleted(
                result?.Outcome.ToString() ?? "Error",
                Stopwatch.GetElapsedTime(
                    startedAt).TotalSeconds);
        }
    }

    private async Task<ExecutionResult> ExecuteCoreAsync(
        ActiveExecution execution,
        JobExecutionOptions options,
        Func<CancellationToken, Task> job,
        CancellationToken cancellationToken)
    {
        bool acquired;

        try
        {
            acquired =
                await TryAcquireLeaseAsync(
                    execution,
                    options.LeaseDuration,
                    cancellationToken);
        }
        catch (OperationCanceledException exception)
            when (cancellationToken.IsCancellationRequested)
        {
            return new ExecutionResult(
                ExecutionOutcome.Cancelled,
                exception);
        }

        if (!acquired)
        {
            return new ExecutionResult(
                ExecutionOutcome.Skipped);
        }

        using var executionTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        using var heartbeatTokenSource =
            new CancellationTokenSource();

        Task<bool>? heartbeatTask = null;
        var result =
            new ExecutionResult(
                ExecutionOutcome.Failed,
                new InvalidOperationException(
                    "The execution ended without producing a result."));

        try
        {
            try
            {
                heartbeatTask =
                    _heartbeatService.RunAsync(
                        execution,
                        options,
                        heartbeatTokenSource.Token);
            }
            catch (Exception exception)
            {
                heartbeatTask =
                    Task.FromException<bool>(
                        exception);
            }

            for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
            {
                if (attempt > 1 && heartbeatTask.IsCompleted)
                {
                    result =
                        ToLeaseLostResult(
                            result,
                            await GetHeartbeatFailureAsync(
                                heartbeatTask));
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    result = new ExecutionResult(
                        ExecutionOutcome.Cancelled,
                        new OperationCanceledException(
                            cancellationToken));
                    break;
                }

                Task jobTask;

                try
                {
                    jobTask =
                        ExecuteJobAsync(
                            execution,
                            job,
                            attempt,
                            executionTokenSource.Token);
                }
                catch (Exception exception)
                {
                    jobTask =
                        Task.FromException(
                            exception);
                }

                var completedTask =
                    await Task.WhenAny(
                        jobTask,
                        heartbeatTask);

                if (completedTask == heartbeatTask)
                {
                    var heartbeatException =
                        await GetHeartbeatFailureAsync(
                            heartbeatTask);

                    executionTokenSource.Cancel();
                    var jobException =
                        await ObserveJobTaskAsync(
                            jobTask,
                            executionTokenSource.Token);

                    result = new ExecutionResult(
                        ExecutionOutcome.LeaseLost,
                        jobException is not null
                            ? new AggregateException(
                                heartbeatException,
                                jobException)
                            : heartbeatException);
                    break;
                }

                result =
                    await GetJobResultAsync(
                        jobTask,
                        cancellationToken);

                if (result.Outcome != ExecutionOutcome.Failed
                    || attempt == options.MaxAttempts
                    || cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                JobGuardianTelemetry.ExecutionRetryScheduled();
                _logger.LogWarning(
                    result.Exception,
                    "Job callback failed; retrying attempt {Attempt} of {MaxAttempts} after {RetryDelay} for {JobKey} execution {ExecutionId}",
                    attempt + 1,
                    options.MaxAttempts,
                    options.RetryDelay,
                    execution.JobKey,
                    execution.ExecutionId);

                if (options.RetryDelay > TimeSpan.Zero)
                {
                    var retryDelayTask =
                        Task.Delay(
                            options.RetryDelay,
                            executionTokenSource.Token);
                    var delayCompletedTask =
                        await Task.WhenAny(
                            retryDelayTask,
                            heartbeatTask);

                    if (delayCompletedTask == heartbeatTask)
                    {
                        result =
                            ToLeaseLostResult(
                                result,
                                await GetHeartbeatFailureAsync(
                                    heartbeatTask));
                        break;
                    }

                    try
                    {
                        await retryDelayTask;
                    }
                    catch (OperationCanceledException exception)
                        when (executionTokenSource.IsCancellationRequested)
                    {
                        result = new ExecutionResult(
                            ExecutionOutcome.Cancelled,
                            exception);
                        break;
                    }
                }
            }
        }
        finally
        {
            heartbeatTokenSource.Cancel();

            if (heartbeatTask is not null)
            {
                try
                {
                    var heartbeatSucceeded =
                        await heartbeatTask;

                    if (!heartbeatSucceeded
                        && result.Outcome != ExecutionOutcome.LeaseLost)
                    {
                        result =
                            ToLeaseLostResult(
                                result,
                                new InvalidOperationException(
                                    "Lease heartbeat stopped before execution completed."));
                    }
                }
                catch (OperationCanceledException)
                    when (heartbeatTokenSource.IsCancellationRequested)
                {
                }
                catch (Exception exception)
                {
                    if (result.Outcome != ExecutionOutcome.LeaseLost)
                    {
                        result =
                            ToLeaseLostResult(
                                result,
                                exception);
                    }
                }
            }

            try
            {
                var released =
                    await ReleaseLeaseAsync(
                        execution);

                if (!released)
                {
                    result =
                        ToLeaseLostResult(
                            result,
                            new InvalidOperationException(
                                "The execution no longer owns the lease during release."));
                }
            }
            catch (Exception exception)
            {
                result =
                    ToLeaseLostResult(
                        result,
                        exception);
            }
        }

        return result;
    }

    private async Task<bool> TryAcquireLeaseAsync(
        ActiveExecution execution,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var startedAt =
            Stopwatch.GetTimestamp();

        var result = "error";

        using var activity =
            JobGuardianTelemetry.StartLeaseActivity(
                "Acquire",
                execution);

        try
        {
            var acquired =
                await _leaseStore.TryAcquireAsync(
                    execution,
                    leaseDuration,
                    cancellationToken);

            result =
                acquired
                    ? "acquired"
                    : "not_acquired";

            if (acquired)
            {
                JobGuardianTelemetry.LeaseAcquired();
            }

            activity?.SetTag(
                "jobguardian.lease.result",
                result);

            return acquired;
        }
        catch (OperationCanceledException exception)
            when (cancellationToken.IsCancellationRequested)
        {
            result = "cancelled";
            activity?.SetTag(
                "jobguardian.lease.result",
                result);
            activity?.SetTag(
                "error.type",
                exception.GetType().FullName);
            throw;
        }
        catch (Exception exception)
        {
            activity?.SetTag(
                "jobguardian.lease.result",
                result);
            activity?.SetTag(
                "error.type",
                exception.GetType().FullName);
            activity?.SetStatus(
                ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            JobGuardianTelemetry.LeaseOperationCompleted(
                "acquire",
                result,
                Stopwatch.GetElapsedTime(
                    startedAt).TotalSeconds);
        }
    }

    private async Task<bool> ReleaseLeaseAsync(
        ActiveExecution execution)
    {
        var startedAt =
            Stopwatch.GetTimestamp();

        var result = "error";

        using var activity =
            JobGuardianTelemetry.StartLeaseActivity(
                "Release",
                execution);

        try
        {
            var released =
                await _leaseStore.ReleaseAsync(
                    execution.JobKey,
                    execution.ExecutionId,
                    CancellationToken.None);

            result =
                released
                    ? "released"
                    : "not_owner";

            activity?.SetTag(
                "jobguardian.lease.result",
                result);

            if (!released)
            {
                activity?.SetStatus(
                    ActivityStatusCode.Error);
            }

            return released;
        }
        catch (Exception exception)
        {
            activity?.SetTag(
                "jobguardian.lease.result",
                result);
            activity?.SetTag(
                "error.type",
                exception.GetType().FullName);
            activity?.SetStatus(
                ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            JobGuardianTelemetry.LeaseReleased();
            JobGuardianTelemetry.LeaseOperationCompleted(
                "release",
                result,
                Stopwatch.GetElapsedTime(
                    startedAt).TotalSeconds);
        }
    }

    private static async Task ExecuteJobAsync(
        ActiveExecution execution,
        Func<CancellationToken, Task> job,
        int attempt,
        CancellationToken cancellationToken)
    {
        using var activity =
            JobGuardianTelemetry.StartActivity(
                "JobGuardian.ExecuteJob",
                execution);
        activity?.SetTag(
            "jobguardian.execution.callback.attempt",
            attempt);

        try
        {
            await job(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            activity?.SetTag(
                "jobguardian.outcome",
                ExecutionOutcome.Cancelled.ToString());
            throw;
        }
        catch (Exception exception)
        {
            activity?.SetTag(
                "error.type",
                exception.GetType().FullName);
            activity?.SetStatus(
                ActivityStatusCode.Error);
            throw;
        }
    }

    private static async Task<ExecutionResult> GetJobResultAsync(
        Task jobTask,
        CancellationToken cancellationToken)
    {
        try
        {
            await jobTask;

            return new ExecutionResult(
                ExecutionOutcome.Succeeded);
        }
        catch (OperationCanceledException exception)
            when (cancellationToken.IsCancellationRequested)
        {
            return new ExecutionResult(
                ExecutionOutcome.Cancelled,
                exception);
        }
        catch (Exception exception)
        {
            return new ExecutionResult(
                ExecutionOutcome.Failed,
                exception);
        }
    }

    private static async Task<Exception> GetHeartbeatFailureAsync(
        Task<bool> heartbeatTask)
    {
        try
        {
            if (await heartbeatTask)
            {
                return new InvalidOperationException(
                    "Lease heartbeat stopped before execution completed.");
            }

            return new InvalidOperationException(
                "Lease heartbeat could not renew the lease.");
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static async Task<Exception?> ObserveJobTaskAsync(
        Task jobTask,
        CancellationToken cancellationToken)
    {
        try
        {
            await jobTask;
            return null;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static ExecutionResult ToLeaseLostResult(
        ExecutionResult result,
        Exception leaseException)
    {
        return new ExecutionResult(
            ExecutionOutcome.LeaseLost,
            result.Exception is null
                ? leaseException
                : new AggregateException(
                    result.Exception,
                    leaseException));
    }
}