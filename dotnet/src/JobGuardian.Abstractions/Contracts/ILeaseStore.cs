using JobGuardian.Abstractions.Models;

namespace JobGuardian.Abstractions.Contracts;

public interface ILeaseStore
{
    Task<bool> TryAcquireAsync(
        ActiveExecution execution,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<bool> RenewAsync(
        JobKey jobKey,
        Guid executionId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<bool> ReleaseAsync(
        JobKey jobKey,
        Guid executionId,
        CancellationToken cancellationToken = default);

    Task<ActiveLease?> GetActiveLeaseAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default);
}