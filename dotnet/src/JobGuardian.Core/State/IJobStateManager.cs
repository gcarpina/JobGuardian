using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.State;

public interface IJobStateManager
{
    Task HandleExecutionResultAsync(
        JobKey jobKey,
        FailurePolicy failurePolicy,
        ExecutionResult executionResult,
        CancellationToken cancellationToken = default);

    Task<JobState> GetCurrentStateAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default);

    Task ResetAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default);
}