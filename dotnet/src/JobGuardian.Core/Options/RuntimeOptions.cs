namespace JobGuardian.Core.Options;

/// <summary>
/// Defines the runtime configuration used by the JobGuardian hosted service.
/// </summary>
public sealed record RuntimeOptions
{
    /// <summary>
    /// Gets or sets the polling interval used to evaluate jobs for execution.
    /// </summary>
    public TimeSpan PollingInterval
    {
        get;
        init;
    }
    = TimeSpan.FromSeconds(30);
}