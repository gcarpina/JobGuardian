using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Policies;

/// <summary>
/// Evaluates an ExecutionResult according to a configured failure policy
/// and produces a PolicyDecision.
///
/// The evaluator performs deterministic policy evaluation only.
///
/// It does not:
/// - mutate job state
/// - schedule executions
/// - persist data
/// - perform administrative actions
/// </summary>
public interface IFailurePolicyEvaluator
{
    PolicyDecision Evaluate(
        ExecutionResult result,
        FailurePolicy policy);
}