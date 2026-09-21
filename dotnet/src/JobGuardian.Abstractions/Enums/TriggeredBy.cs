namespace JobGuardian.Abstractions.Enums;

/// <summary>
/// Describes the specific entity or system that initiated a job execution.
/// </summary>
public enum TriggeredBy
{
    /// <summary>
    /// The execution was initiated by the scheduling mechanism.
    /// </summary>
    Scheduler,

    /// <summary>
    /// The execution was initiated by an operator.
    /// </summary>
    Operator,

    /// <summary>
    /// The execution was initiated by a recovery policy.
    /// </summary>
    RecoveryPolicy,

    /// <summary>
    /// The execution was initiated through an API entry point.
    /// </summary>
    Api
}