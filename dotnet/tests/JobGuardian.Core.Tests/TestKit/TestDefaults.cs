using JobGuardian.Core.Models;
using JobGuardian.Core.Options;

namespace JobGuardian.Core.Tests.TestKit;

internal static class TestDefaults
{
    public static readonly RuntimeOptions
        DefaultRuntimeOptions =
            new()
            {
                PollingInterval =
                    TimeSpan.FromSeconds(30)
            };

    public static readonly RuntimeOptions
        FastRuntimeOptions =
            new()
            {
                PollingInterval =
                    TimeSpan.FromMilliseconds(50)
            };

    public static readonly JobExecutionPolicy
        DefaultPolicy =
            new()
            {
                LeaseDuration =
                    TimeSpan.FromMinutes(5),

                HeartbeatInterval =
                    TimeSpan.FromSeconds(30)
            };
}