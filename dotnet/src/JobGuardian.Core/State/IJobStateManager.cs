using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.State;

/// <summary>
/// Applies execution outcomes to a job's operational state and exposes state queries.
/// </summary>
/// <remarks>
/// State management is the coordination layer that translates execution results into the current
/// execution eligibility state for a job.
/// </remarks>
public interface IJobStateManager
{
    /// <summary>
    /// Applies the result of an execution to the job's current state.
    /// </summary>
    /// <param name="jobKey">
    /// The logical identity of the job whose state should be updated.
    /// </param>
    /// <param name="failurePolicy">
    /// The failure policy that determines how the execution result should affect eligibility.
    /// </param>
    /// <param name="executionResult">
    /// The result produced by the completed execution.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    Task HandleExecutionResultAsync(
        JobKey jobKey,
        FailurePolicy failurePolicy,
        ExecutionResult executionResult,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state for the specified job.
    /// </summary>
    /// <param name="jobKey">
    /// The logical identity of the job whose state is requested.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The current job state.
    /// </returns>
    Task<JobState> GetCurrentStateAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the job to the eligible state.
    /// </summary>
    /// <param name="jobKey">
    /// The job whose state should be reset.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    Task ResetAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default);
}