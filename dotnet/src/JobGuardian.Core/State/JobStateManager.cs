using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Policies;

namespace JobGuardian.Core.State;

using JobGuardian.Abstractions.Contracts;

/// <summary>
/// Applies execution outcomes to job state and exposes the current eligibility state.
/// </summary>
public sealed class JobStateManager
    : IJobStateManager
{
    private readonly IFailurePolicyEvaluator _policyEvaluator;

    private readonly IJobStateTransitionEngine _transitionEngine;

    private readonly IJobStateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStateManager"/> class.
    /// </summary>
    /// <param name="policyEvaluator">
    /// Evaluates execution results against the configured failure policy.
    /// </param>
    /// <param name="transitionEngine">
    /// Applies policy decisions to a job's current state.
    /// </param>
    /// <param name="repository">
    /// Stores the current state for each job key.
    /// </param>
    public JobStateManager(
        IFailurePolicyEvaluator policyEvaluator,
        IJobStateTransitionEngine transitionEngine,
        IJobStateRepository repository)
    {
        _policyEvaluator = policyEvaluator;
        _transitionEngine = transitionEngine;
        _repository = repository;
    }

    /// <summary>
    /// Applies the result of a completed execution to the job's current state.
    /// </summary>
    /// <param name="jobKey">
    /// The logical identity of the job whose state should be updated.
    /// </param>
    /// <param name="failurePolicy">
    /// The policy used to evaluate the execution result.
    /// </param>
    /// <param name="executionResult">
    /// The execution result produced by the completed run.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    public async Task HandleExecutionResultAsync(
        JobKey jobKey,
        FailurePolicy failurePolicy,
        ExecutionResult executionResult,
        CancellationToken cancellationToken = default)
    {
        var currentState =
            await GetCurrentStateAsync(
                jobKey,
                cancellationToken);

        var decision =
            _policyEvaluator.Evaluate(
                executionResult,
                failurePolicy);

        var newState =
            _transitionEngine.Apply(
                currentState == JobState.Eligible
                    ? JobState.Running
                    : currentState,
                decision);

        if (newState == JobState.Eligible)
        {
            await _repository.SetAsync(
                jobKey,
                JobState.Eligible,
                cancellationToken);

            return;
        }

        await _repository.SetAsync(
            jobKey,
            newState,
            cancellationToken);
    }

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
    /// The current state, or <see cref="JobState.Eligible"/> when no state is persisted.
    /// </returns>
    public async Task<JobState> GetCurrentStateAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default)
    {
        var state =
            await _repository.GetAsync(
                jobKey,
                cancellationToken);

        return state ?? JobState.Eligible;
    }

    /// <summary>
    /// Resets the job to the eligible state.
    /// </summary>
    /// <param name="jobKey">
    /// The logical identity of the job to reset.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    public async Task ResetAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default)
    {
        await _repository.SetAsync(
            jobKey,
            JobState.Eligible,
            cancellationToken);
    }
}