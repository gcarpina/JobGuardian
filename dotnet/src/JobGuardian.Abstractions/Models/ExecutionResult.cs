using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Abstractions.Models;

public sealed record ExecutionResult(
    ExecutionOutcome Outcome,
    Exception? Exception = null);