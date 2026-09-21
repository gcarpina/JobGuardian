using JobGuardian.Abstractions.Contracts;
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
    /// <c>true</c> when the job completes successfully under an active lease; otherwise, <c>false</c>
    /// when the lease is unavailable or cannot be maintained.
    /// </returns>
    public async Task<bool> ExecuteAsync(
        ActiveExecution execution,
        JobExecutionOptions options,
        Func<CancellationToken, Task> job,
        CancellationToken cancellationToken = default)
    {
        var acquired =
            await _leaseStore.TryAcquireAsync(
                execution,
                options.LeaseDuration,
                cancellationToken);

        if (!acquired)
        {
            return false;
        }

        using var executionTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        using var heartbeatTokenSource =
            new CancellationTokenSource();

        var heartbeatTask =
            _heartbeatService.RunAsync(
                execution,
                options,
                heartbeatTokenSource.Token);

        try
        {
            var jobTask =
                job(
                    executionTokenSource.Token);

            var completedTask =
                await Task.WhenAny(
                    jobTask,
                    heartbeatTask);

            if (completedTask == heartbeatTask)
            {
                var heartbeatSucceeded =
                    await heartbeatTask;

                if (!heartbeatSucceeded)
                {
                    executionTokenSource.Cancel();

                    try
                    {
                        await jobTask;
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    return false;
                }
            }

            await jobTask;

            return true;
        }
        finally
        {
            heartbeatTokenSource.Cancel();

            try
            {
                await heartbeatTask;
            }
            catch (OperationCanceledException)
            {
            }

            await _leaseStore.ReleaseAsync(
                execution.JobKey,
                execution.ExecutionId,
                cancellationToken);
        }
    }
}