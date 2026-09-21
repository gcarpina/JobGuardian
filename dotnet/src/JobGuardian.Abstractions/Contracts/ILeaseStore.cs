using JobGuardian.Abstractions.Models;

namespace JobGuardian.Abstractions.Contracts;

/// <summary>
/// Coordinates distributed lease ownership for job execution across multiple application instances.
/// </summary>
/// <remarks>
/// Lease acquisition and renewal are the core mechanism JobGuardian uses to ensure that only one
/// active owner can execute a given job at a time.
/// </remarks>
public interface ILeaseStore
{
    /// <summary>
    /// Attempts to acquire a lease for the specified execution.
    /// </summary>
    /// <param name="execution">
    /// Information about the execution requesting ownership.
    /// </param>
    /// <param name="leaseDuration">
    /// Duration for which the lease should remain valid.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// <c>true</c> when the lease is acquired; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> TryAcquireAsync(
        ActiveExecution execution,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renews the lease for the specified execution while it is still active.
    /// </summary>
    /// <param name="jobKey">
    /// The job whose lease is being renewed.
    /// </param>
    /// <param name="executionId">
    /// The execution identifier associated with the current owner.
    /// </param>
    /// <param name="leaseDuration">
    /// The renewed duration for the lease.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// <c>true</c> when the lease is renewed; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> RenewAsync(
        JobKey jobKey,
        Guid executionId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the lease owned by the specified execution.
    /// </summary>
    /// <param name="jobKey">
    /// The job whose lease should be released.
    /// </param>
    /// <param name="executionId">
    /// The execution identifier of the lease owner.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// <c>true</c> when the lease is released; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> ReleaseAsync(
        JobKey jobKey,
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active lease for the specified job, if one exists.
    /// </summary>
    /// <param name="jobKey">
    /// The job whose active lease is requested.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The current active lease, or <c>null</c> when no valid lease is active.
    /// </returns>
    Task<ActiveLease?> GetActiveLeaseAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default);
}