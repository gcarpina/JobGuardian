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
}