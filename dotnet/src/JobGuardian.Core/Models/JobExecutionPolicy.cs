using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Core.Models;

/// <summary>
/// Defines the lease and heartbeat policy associated with a job registration.
/// </summary>
public sealed record JobExecutionPolicy
{
    /// <summary>
    /// Gets the duration for which an execution may hold the lease before it expires.
    /// </summary>
    public required TimeSpan LeaseDuration
    {
        get;
        init;
    }

    /// <summary>
    /// Gets the interval at which heartbeat renewals should occur while the job is executing.
    /// </summary>
    public required TimeSpan HeartbeatInterval
    {
        get;
        init;
    }

    /// <summary>
    /// Gets the maximum number of callback attempts for one coordinated execution.
    /// </summary>
    /// <remarks>
    /// The initial callback invocation counts as an attempt. The default of one preserves
    /// the behavior without retries. Values from 1 through 10 are accepted.
    /// </remarks>
    public int MaxAttempts
    {
        get;
        init;
    } = 1;

    /// <summary>
    /// Gets the fixed delay between failed callback attempts.
    /// </summary>
    /// <remarks>
    /// The default is one second. Values from zero through five minutes are accepted.
    /// </remarks>
    public TimeSpan RetryDelay
    {
        get;
        init;
    } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets the policy used to determine job eligibility after an execution completes.
    /// </summary>
    public FailurePolicy FailurePolicy
    {
        get;
        init;
    } = JobGuardian.Abstractions.Enums.FailurePolicy.Ignore;
}