namespace JobGuardian.Core.Options;

public sealed record RuntimeOptions
{
    public TimeSpan PollingInterval
    {
        get;
        init;
    }
    = TimeSpan.FromSeconds(30);
}