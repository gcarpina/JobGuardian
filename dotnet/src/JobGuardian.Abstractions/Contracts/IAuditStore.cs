using JobGuardian.Abstractions.Models;

namespace JobGuardian.Abstractions.Contracts;

/// <summary>
/// Persists audit records for supported JobGuardian administrative operations.
/// </summary>
public interface IAuditStore
{
    /// <summary>
    /// Writes an audit entry to the underlying storage mechanism.
    /// </summary>
    /// <param name="entry">
    /// The audit entry to persist.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    Task WriteAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default);
}