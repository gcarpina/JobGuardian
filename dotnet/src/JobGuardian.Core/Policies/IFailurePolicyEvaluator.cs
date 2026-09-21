using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Policies;

/// <summary>
/// Evaluates an execution result against a configured failure policy.
/// </summary>
/// <remarks>
/// The evaluator performs deterministic policy evaluation only and does not mutate job state,
/// schedule executions, persist data, or perform administrative actions.
/// </remarks>
public interface IFailurePolicyEvaluator
{
    /// <summary>
    /// Evaluates the supplied result and returns the policy decision that should be applied.
    /// </summary>
    /// <param name="result">
    /// The execution result to evaluate.
    /// </param>
    /// <param name="policy">
    /// The failure policy that governs the evaluation.
    /// </param>
    /// <returns>
    /// The policy decision derived from the execution result.
    /// </returns>
    PolicyDecision Evaluate(
        ExecutionResult result,
        FailurePolicy policy);
}