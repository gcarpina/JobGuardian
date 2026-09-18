namespace JobGuardian.Abstractions.Enums;

/// <summary>
/// Represents the final outcome of a job execution attempt.
/// </summary>
public enum ExecutionOutcome
{
    /// <summary>
    /// The job completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The job execution failed because an exception occurred.
    /// </summary>
    Failed,

    /// <summary>
    /// The job execution was cancelled before completion.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The job execution could not continue because lease ownership was lost.
    /// </summary>
    LeaseLost,

    /// <summary>
    /// The job execution was skipped because lease ownership could not be acquired.
    /// </summary>
    Skipped
}