using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Policies;

namespace JobGuardian.Core.State;

using JobGuardian.Abstractions.Contracts;

public sealed class JobStateManager
    : IJobStateManager
{
    private readonly IFailurePolicyEvaluator _policyEvaluator;

    private readonly IJobStateTransitionEngine _transitionEngine;

    private readonly IJobStateRepository _repository;

    public JobStateManager(
        IFailurePolicyEvaluator policyEvaluator,
        IJobStateTransitionEngine transitionEngine,
        IJobStateRepository repository)
    {
        _policyEvaluator = policyEvaluator;
        _transitionEngine = transitionEngine;
        _repository = repository;
    }

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