using JobGuardian.Abstractions.Models;

using JobGuardian.Core.Models;

namespace JobGuardian.Core.Contracts;

/// <summary>
/// Periodically renews an active lease while a job is executing.
/// </summary>
/// <remarks>
/// The heartbeat service keeps the execution lease alive, allowing JobGuardian to detect loss of
/// ownership and stop work when the active owner no longer holds the lease.
/// </remarks>
public interface ILeaseHeartbeatService
{
    /// <summary>
    /// Runs the lease heartbeat loop for the specified execution.
    /// </summary>
    /// <param name="execution">
    /// The execution whose lease should be maintained.
    /// </param>
    /// <param name="options">
    /// The execution options that define the lease renewal cadence.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// <c>true</c> when the heartbeat loop completes normally; otherwise, <c>false</c> if the
    /// lease can no longer be renewed.
    /// </returns>
    Task<bool> RunAsync(
        ActiveExecution execution,
        JobExecutionOptions options,
        CancellationToken cancellationToken = default);
}