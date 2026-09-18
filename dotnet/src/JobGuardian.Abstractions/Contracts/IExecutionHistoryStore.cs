using JobGuardian.Abstractions.Models;

namespace JobGuardian.Abstractions.Contracts;

public interface IExecutionHistoryStore
{
    Task CreateAsync(
        ExecutionHistoryEntry entry,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ExecutionHistoryEntry entry,
        CancellationToken cancellationToken = default);

    Task<ExecutionHistoryEntry?> GetAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);
}