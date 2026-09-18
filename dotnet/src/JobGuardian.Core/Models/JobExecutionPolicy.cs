namespace JobGuardian.Core.Models;

public sealed record JobExecutionPolicy
{
    public required TimeSpan LeaseDuration
    {
        get;
        init;
    }

    public required TimeSpan HeartbeatInterval
    {
        get;
        init;
    }
}