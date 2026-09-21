namespace JobGuardian.Abstractions.Enums;

/// <summary>
/// Describes how JobGuardian responds when a job fails or loses its lease.
/// </summary>
public enum FailurePolicy
{
    /// <summary>
    /// Continues to allow future executions without blocking the job.
    /// </summary>
    Ignore,

    /// <summary>
    /// Blocks the job until an operator manually resets execution eligibility.
    /// </summary>
    RequireManualReset
}