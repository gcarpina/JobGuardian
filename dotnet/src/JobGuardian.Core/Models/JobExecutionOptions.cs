namespace JobGuardian.Core.Models;

/// <summary>
/// Defines the runtime lease and heartbeat settings for a single job execution.
/// </summary>
public sealed record JobExecutionOptions
{
    /// <summary>
    /// Gets the amount of time the execution may hold its lease before it expires.
    /// </summary>
    public required TimeSpan LeaseDuration
    {
        get;
        init;
    }

    /// <summary>
    /// Gets the interval at which lease renewal heartbeats are sent while the job runs.
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
    /// The initial callback invocation counts as an attempt. Values from 1 through 10 are accepted.
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
}