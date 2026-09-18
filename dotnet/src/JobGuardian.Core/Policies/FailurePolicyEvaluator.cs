using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Policies;

public sealed class FailurePolicyEvaluator
    : IFailurePolicyEvaluator
{
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