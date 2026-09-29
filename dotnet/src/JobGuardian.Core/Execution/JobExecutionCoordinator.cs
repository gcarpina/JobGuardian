using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Contracts;
using JobGuardian.Core.Models;

namespace JobGuardian.Core.Execution;

/// <summary>
/// Executes a job while coordinating lease acquisition, heartbeat renewal, and cleanup.
/// </summary>
public sealed class JobExecutionCoordinator
    : IJobExecutionCoordinator
{
    private readonly ILeaseStore _leaseStore;

    private readonly ILeaseHeartbeatService _heartbeatService;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobExecutionCoordinator"/> class.
    /// </summary>
    /// <param name="leaseStore">
    /// The lease store used to acquire and release execution ownership.
    /// </param>
    /// <param name="heartbeatService">
    /// The heartbeat service used to renew the lease while the job runs.
    /// </param>
    public JobExecutionCoordinator(
        ILeaseStore leaseStore,
        ILeaseHeartbeatService heartbeatService)
    {
        _leaseStore = leaseStore;
        _heartbeatService = heartbeatService;
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
        bool acquired;

        try
        {
            acquired =
                await _leaseStore.TryAcquireAsync(
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

            Task jobTask;

            try
            {
                jobTask =
                    job(
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
                bool heartbeatSucceeded;
                Exception? heartbeatException = null;

                try
                {
                    heartbeatSucceeded =
                        await heartbeatTask;
                }
                catch (OperationCanceledException)
                    when (heartbeatTokenSource.IsCancellationRequested)
                {
                    heartbeatSucceeded = true;
                }
                catch (OperationCanceledException exception)
                {
                    heartbeatSucceeded = false;
                    heartbeatException = exception;
                }
                catch (Exception exception)
                {
                    heartbeatSucceeded = false;
                    heartbeatException = exception;
                }

                if (!heartbeatSucceeded)
                {
                    executionTokenSource.Cancel();
                    Exception? jobException = null;

                    try
                    {
                        await jobTask;
                    }
                    catch (OperationCanceledException)
                        when (executionTokenSource.IsCancellationRequested)
                    {
                    }
                    catch (Exception exception)
                    {
                        jobException = exception;
                    }

                    result = new ExecutionResult(
                        ExecutionOutcome.LeaseLost,
                        heartbeatException is not null && jobException is not null
                            ? new AggregateException(
                                heartbeatException,
                                jobException)
                            : heartbeatException ?? jobException);
                }

                if (heartbeatSucceeded)
                {
                    result =
                        await GetJobResultAsync(
                            jobTask,
                            cancellationToken);
                }
            }
            else
            {
                result =
                    await GetJobResultAsync(
                        jobTask,
                        cancellationToken);
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

                    if (!heartbeatSucceeded)
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
                    result =
                        ToLeaseLostResult(
                            result,
                            exception);
                }
            }

            try
            {
                var released =
                    await _leaseStore.ReleaseAsync(
                        execution.JobKey,
                        execution.ExecutionId,
                        CancellationToken.None);

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