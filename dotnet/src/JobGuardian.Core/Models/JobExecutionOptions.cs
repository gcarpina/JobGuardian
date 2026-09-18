namespace JobGuardian.Core.Models;

public sealed record JobExecutionOptions
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