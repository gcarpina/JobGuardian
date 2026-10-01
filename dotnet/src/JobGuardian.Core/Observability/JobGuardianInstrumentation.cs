namespace JobGuardian.Core.Observability;

/// <summary>
/// Names of the OpenTelemetry instrumentation sources published by JobGuardian.Core.
/// </summary>
/// <remarks>
/// Configure OpenTelemetry to listen to both names to receive JobGuardian traces and metrics.
/// </remarks>
public static class JobGuardianInstrumentation
{
    /// <summary>
    /// Gets the meter name used for JobGuardian metrics.
    /// </summary>
    public const string MeterName = "JobGuardian";

    /// <summary>
    /// Gets the activity source name used for JobGuardian traces.
    /// </summary>
    public const string ActivitySourceName = "JobGuardian";
}
