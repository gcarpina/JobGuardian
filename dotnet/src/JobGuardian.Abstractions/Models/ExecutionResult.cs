using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Abstractions.Models;

/// <summary>
/// Represents the outcome of a completed or interrupted execution.
/// </summary>
/// <remarks>
/// The result captures the observed outcome and, when present, the exception that caused the
/// execution to fail or be interrupted.
/// </remarks>
/// <param name="Outcome">The observed terminal outcome.</param>
/// <param name="Exception">The exception associated with the outcome, when available.</param>
public sealed record ExecutionResult(
    ExecutionOutcome Outcome,
    Exception? Exception = null)
{
    /// <summary>
    /// Gets the stable reason code for outcomes such as <see cref="ExecutionOutcome.Skipped"/>.
    /// </summary>
    public ExecutionOutcomeReason? OutcomeReason { get; init; }
}