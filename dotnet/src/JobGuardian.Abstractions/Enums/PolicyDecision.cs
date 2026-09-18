namespace JobGuardian.Abstractions.Enums;

/// <summary>
/// Represents the result of failure policy evaluation.
///
/// A policy decision determines how execution eligibility should evolve
/// after evaluating an ExecutionResult.
///
/// PolicyDecision expresses intent only and does not perform runtime actions,
/// state mutations, scheduling or persistence.
/// </summary>
public enum PolicyDecision
{
    /// <summary>
    /// The job remains eligible for future executions.
    /// </summary>
    Continue,

    /// <summary>
    /// The job becomes blocked and requires administrative intervention
    /// before new executions are allowed.
    /// </summary>
    Block
}