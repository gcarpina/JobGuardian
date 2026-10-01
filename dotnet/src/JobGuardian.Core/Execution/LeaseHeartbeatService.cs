using System.Diagnostics;
using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;

using JobGuardian.Core.Contracts;
using JobGuardian.Core.Models;
using JobGuardian.Core.Observability;

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
            var startedAt =
                Stopwatch.GetTimestamp();

            var result = "error";

            using var activity =
                JobGuardianTelemetry.StartLeaseActivity(
                    "Renew",
                    execution);

            bool renewed;

            try
            {
                renewed =
                    await _leaseStore.RenewAsync(
                        execution.JobKey,
                        execution.ExecutionId,
                        options.LeaseDuration,
                        cancellationToken);

                result =
                    renewed
                        ? "renewed"
                        : "not_owner";

                activity?.SetTag(
                    "jobguardian.lease.result",
                    result);

                if (!renewed)
                {
                    activity?.SetStatus(
                        ActivityStatusCode.Error);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                result = "cancelled";
                activity?.SetTag(
                    "jobguardian.lease.result",
                    result);
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
                    "renew",
                    result,
                    Stopwatch.GetElapsedTime(
                        startedAt).TotalSeconds);
            }

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
