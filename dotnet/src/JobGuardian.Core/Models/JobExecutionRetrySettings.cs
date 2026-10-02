namespace JobGuardian.Core.Models;

internal static class JobExecutionRetrySettings
{
    internal const int MaximumAttempts = 10;

    internal static readonly TimeSpan MaximumRetryDelay =
        TimeSpan.FromMinutes(5);

    internal static void Validate(
        int maxAttempts,
        TimeSpan retryDelay)
    {
        if (maxAttempts is < 1 or > MaximumAttempts)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxAttempts),
                maxAttempts,
                $"The value must be between 1 and {MaximumAttempts}.");
        }

        if (retryDelay < TimeSpan.Zero
            || retryDelay > MaximumRetryDelay)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retryDelay),
                retryDelay,
                $"The value must be between zero and {MaximumRetryDelay}.");
        }
    }
}
