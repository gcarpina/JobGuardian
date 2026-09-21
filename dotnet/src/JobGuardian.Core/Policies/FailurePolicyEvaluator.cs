using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Policies;

/// <summary>
/// Evaluates a job execution result against a configured failure policy.
/// </summary>
public sealed class FailurePolicyEvaluator
    : IFailurePolicyEvaluator
{
    /// <summary>
    /// Evaluates the supplied execution result and returns the corresponding policy decision.
    /// </summary>
    /// <param name="result">
    /// The execution result to evaluate.
    /// </param>
    /// <param name="policy">
    /// The failure policy to apply.
    /// </param>
    /// <returns>
    /// A decision indicating whether the job should continue or block.
    /// </returns>
    public PolicyDecision Evaluate(
        ExecutionResult result,
        FailurePolicy policy)
    {
        return policy switch
        {
            FailurePolicy.Ignore =>
                PolicyDecision.Continue,

            FailurePolicy.RequireManualReset =>
                EvaluateRequireManualReset(
                    result),

            _ => throw new ArgumentOutOfRangeException(
                nameof(policy),
                policy,
                "Unsupported failure policy.")
        };
    }

    private static PolicyDecision EvaluateRequireManualReset(
        ExecutionResult result)
    {
        return result.Outcome switch
        {
            ExecutionOutcome.Failed =>
                PolicyDecision.Block,

            ExecutionOutcome.LeaseLost =>
                PolicyDecision.Block,

            ExecutionOutcome.Succeeded =>
                PolicyDecision.Continue,

            ExecutionOutcome.Cancelled =>
                PolicyDecision.Continue,

            ExecutionOutcome.Skipped =>
                PolicyDecision.Continue,

            _ => throw new ArgumentOutOfRangeException(
                nameof(result),
                result.Outcome,
                "Unsupported execution outcome.")
        };
    }
}