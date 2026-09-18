namespace JobGuardian.Abstractions.Enums;

/// <summary>
/// Represents the operational execution eligibility of a job.
///
/// JobState answers the question:
///
/// "Can this job be executed now?"
///
/// JobState is independent from:
/// - ExecutionOutcome
/// - FailurePolicy
/// - ExecutionHistory
///
/// State transitions are applied through the
/// Job State Transition Engine.
/// </summary>
public enum JobState
{
    /// <summary>
    /// The job can be executed.
    /// </summary>
    Eligible,

    /// <summary>
    /// The job is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// The job cannot be executed until it is explicitly unblocked.
    /// </summary>
    Blocked
}