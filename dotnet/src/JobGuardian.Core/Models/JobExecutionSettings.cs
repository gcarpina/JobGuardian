namespace JobGuardian.Core.Models;

internal static class JobExecutionSettings
{
    internal static void Validate(
        TimeSpan leaseDuration,
        TimeSpan heartbeatInterval)
    {
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseDuration),
                "Lease duration must be greater than zero.");
        }

        if (heartbeatInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(heartbeatInterval),
                "Heartbeat interval must be greater than zero.");
        }

        if (heartbeatInterval >= leaseDuration)
        {
            throw new ArgumentException(
                "Heartbeat interval must be less than lease duration.",
                nameof(heartbeatInterval));
        }
    }
}
