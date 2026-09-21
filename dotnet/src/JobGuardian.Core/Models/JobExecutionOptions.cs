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
}