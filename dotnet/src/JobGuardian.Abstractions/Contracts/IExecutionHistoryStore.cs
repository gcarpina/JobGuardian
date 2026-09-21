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
}