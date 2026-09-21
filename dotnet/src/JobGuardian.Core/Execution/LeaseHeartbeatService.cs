using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;

using JobGuardian.Core.Contracts;
using JobGuardian.Core.Models;

namespace JobGuardian.Core.Execution;

/// <summary>
/// Keeps a lease alive while the owning execution continues to process work.
/// </summary>
public sealed class LeaseHeartbeatService
    : ILeaseHeartbeatService
{
    private readonly ILeaseStore _leaseStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeaseHeartbeatService"/> class.
    /// </summary>
    /// <param name="leaseStore">
    /// The store used to renew the active lease.
    /// </param>
    public LeaseHeartbeatService(
        ILeaseStore leaseStore)
    {
        _leaseStore = leaseStore;
    }

    /// <summary>
    /// Runs the lease renewal loop for the active execution.
    /// </summary>
    /// <param name="execution">
    /// The execution whose lease should remain active.
    /// </param>
    /// <param name="options">
    /// The lease and heartbeat settings used to renew the lease.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the heartbeat loop.
    /// </param>
    /// <returns>
    /// <c>true</c> when the heartbeat loop exits normally; otherwise, <c>false</c> when the lease
    /// can no longer be renewed.
    /// </returns>
    public async Task<bool> RunAsync(
        ActiveExecution execution,
        JobExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var renewed =
                await _leaseStore.RenewAsync(
                    execution.JobKey,
                    execution.ExecutionId,
                    options.LeaseDuration,
                    cancellationToken);

            if (!renewed)
            {
                return false;
            }

            try
            {
                await Task.Delay(
                    options.HeartbeatInterval,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return true;
    }
}
