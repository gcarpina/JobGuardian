using JobGuardian.Abstractions.Models;

namespace JobGuardian.Abstractions.Contracts;

/// <summary>
/// Persists and retrieves the execution history for a job execution.
/// </summary>
/// <remarks>
/// JobGuardian uses execution history to retain the operational record of each run,
/// including lifecycle metadata, outcome, and any failure context.
/// </remarks>
public interface IExecutionHistoryStore
{
    /// <summary>
    /// Creates a new execution history record.
    /// </summary>
    /// <param name="entry">
    /// The execution history record to persist.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    Task CreateAsync(
        ExecutionHistoryEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing execution history record.
    /// </summary>
    /// <param name="entry">
    /// The updated execution history record.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    Task UpdateAsync(
        ExecutionHistoryEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the execution history for the specified execution.
    /// </summary>
    /// <param name="executionId">
    /// The execution identifier to look up.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The execution history entry, or <c>null</c> when no record exists.
    /// </returns>
    Task<ExecutionHistoryEntry?> GetAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries execution history for one job and a bounded start-time interval.
    /// </summary>
    /// <param name="query">The tenant-scoped filters and pagination settings.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A page of matching history entries.</returns>
    Task<ExecutionHistoryPage> QueryAsync(
        ExecutionHistoryQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the specified execution as having acquired its lease.
    /// </summary>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<DateTimeOffset> MarkLeaseAcquiredAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks earlier acquired executions with no terminal outcome as abandoned.
    /// </summary>
    /// <remarks>
    /// The caller must invoke this only after a later execution has acquired the same job lease.
    /// </remarks>
    /// <param name="jobKey">The tenant-scoped job identity.</param>
    /// <param name="currentExecutionId">The newly acquired execution to exclude.</param>
    /// <param name="endedAtUtc">The UTC recovery timestamp.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<int> MarkPreviousIncompleteAsAbandonedAsync(
        JobKey jobKey,
        Guid currentExecutionId,
        DateTimeOffset endedAtUtc,
        CancellationToken cancellationToken = default);
}