namespace JobGuardian.Abstractions.Enums;

/// <summary>
/// Stable reason codes that qualify an execution outcome.
/// </summary>
public enum ExecutionOutcomeReason
{
    /// <summary>
    /// The job could not acquire a lease because another owner held a valid lease.
    /// </summary>
    LeaseBusy,

    /// <summary>
    /// The job was skipped because its persisted state is blocked.
    /// </summary>
    Blocked,

    /// <summary>
    /// A previously acquired execution had no terminal outcome when its lease expired.
    /// </summary>
    LeaseExpiredWithoutOutcome
}
