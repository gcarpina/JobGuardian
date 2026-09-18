using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;

using JobGuardian.Core.Contracts;
using JobGuardian.Core.Models;

namespace JobGuardian.Core.Execution;

public sealed class LeaseHeartbeatService
    : ILeaseHeartbeatService
{
    private readonly ILeaseStore _leaseStore;

    public LeaseHeartbeatService(
        ILeaseStore leaseStore)
    {
        _leaseStore = leaseStore;
    }

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
                // heartbeat was cancelled, exit loop
                break;
            }
        }

        return true;
    }
}
