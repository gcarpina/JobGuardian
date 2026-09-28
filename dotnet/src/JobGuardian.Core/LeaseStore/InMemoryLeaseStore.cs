using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.LeaseStore;

/// <summary>
/// Coordinates job leases within a single process.
/// </summary>
/// <remarks>
/// This store is intended for local development and single-process scenarios. Its leases are not
/// shared across application instances and are lost when the process exits.
/// </remarks>
public sealed class InMemoryLeaseStore
    : ILeaseStore
{
    private readonly object _sync = new();

    private readonly Dictionary<JobKey, ActiveLease> _leases = [];

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
    public Task<bool> TryAcquireAsync(
        ActiveExecution execution,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;

        lock (_sync)
        {
            if (_leases.TryGetValue(
                    execution.JobKey,
                    out var currentLease)
                && currentLease.LeaseUntilUtc >= now)
            {
                return Task.FromResult(false);
            }

            _leases[execution.JobKey] =
                new ActiveLease
                {
                    JobKey = execution.JobKey,
                    ExecutionId = execution.ExecutionId,
                    OwnerId = execution.OwnerId,
                    AcquiredAtUtc = now,
                    RenewedAtUtc = now,
                    LeaseUntilUtc = now + leaseDuration
                };
        }

        return Task.FromResult(true);
    }

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
    public Task<bool> RenewAsync(
        JobKey jobKey,
        Guid executionId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;

        lock (_sync)
        {
            if (!_leases.TryGetValue(
                    jobKey,
                    out var currentLease)
                || currentLease.ExecutionId != executionId
                || currentLease.LeaseUntilUtc < now)
            {
                return Task.FromResult(false);
            }

            _leases[jobKey] =
                currentLease with
                {
                    RenewedAtUtc = now,
                    LeaseUntilUtc = now + leaseDuration
                };
        }

        return Task.FromResult(true);
    }

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
    public Task<bool> ReleaseAsync(
        JobKey jobKey,
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_leases.TryGetValue(
                    jobKey,
                    out var currentLease)
                || currentLease.ExecutionId != executionId)
            {
                return Task.FromResult(false);
            }

            _leases.Remove(jobKey);
        }

        return Task.FromResult(true);
    }

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
    public Task<ActiveLease?> GetActiveLeaseAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_leases.TryGetValue(
                    jobKey,
                    out var lease)
                && lease.LeaseUntilUtc >= DateTimeOffset.UtcNow)
            {
                return Task.FromResult<ActiveLease?>(
                    lease);
            }
        }

        return Task.FromResult<ActiveLease?>(
            null);
    }
}
