using System.Collections.Concurrent;
using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Audit;

/// <summary>
/// Stores audit entries in memory for local and test scenarios.
/// </summary>
/// <remarks>
/// Entries are lost when the process exits. Use a persistent audit provider for operational
/// deployments that require durable audit history.
/// </remarks>
public sealed class InMemoryAuditStore
    : IAuditStore
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();

    /// <summary>
    /// Gets a snapshot of the audit entries currently held by this store.
    /// </summary>
    public IReadOnlyCollection<AuditEntry> Entries
        => _entries.ToArray();

    /// <summary>
    /// Adds an audit entry to the in-memory store.
    /// </summary>
    /// <param name="entry">
    /// The audit entry to persist.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    public Task WriteAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();
        _entries.Enqueue(entry);
        return Task.CompletedTask;
    }
}
