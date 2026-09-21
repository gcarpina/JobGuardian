using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Abstractions.Models;

/// <summary>
/// Represents the outcome of a completed or interrupted execution.
/// </summary>
/// <remarks>
/// The result captures the observed outcome and, when present, the exception that caused the
/// execution to fail or be interrupted.
/// </remarks>
public sealed record ExecutionResult(
    ExecutionOutcome Outcome,
    Exception? Exception = null);