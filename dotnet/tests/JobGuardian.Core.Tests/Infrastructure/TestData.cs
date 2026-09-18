using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Models;

namespace JobGuardian.Core.Tests.Infrastructure;

internal static class TestData
{
    public const string TenantId =
        "default";

    public const string JobNamespace =
        "finance";

    public const string JobName =
        "nightly-import";

    public static readonly TimeSpan LeaseDuration =
        TimeSpan.FromSeconds(30);

    public static ActiveExecution CreateExecution(
        string ownerId)
    {
        return new ActiveExecution
        {
            JobKey =
                new JobKey(
                    TenantId,
                    JobNamespace,
                    JobName),

            ExecutionId =
                Guid.NewGuid(),

            OwnerId =
                ownerId
        };
    }

    public static JobExecutionOptions CreateOptions()
    {
        return new JobExecutionOptions
        {
            LeaseDuration =
                LeaseDuration,

            HeartbeatInterval =
                TimeSpan.FromMilliseconds(10)
        };
    }
}