namespace JobGuardian.Abstractions.Enums;

/// <summary>
/// Describes the source or trigger type for a job execution.
/// </summary>
public enum RunType
{
    /// <summary>
    /// The execution was triggered by the standard scheduled workflow.
    /// </summary>
    Scheduled,

    /// <summary>
    /// The execution was triggered by a manual operator action.
    /// </summary>
    Manual,

    /// <summary>
    /// The execution was triggered as part of recovery logic.
    /// </summary>
    Recovery
}