using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Options;

public sealed record ExecutionOptions
{
    public required JobKey JobKey { get; init; }

    public required string ApplicationName { get; init; }

    public required string Environment { get; init; }

    public required OwnerId OwnerId { get; init; }

    public TimeSpan LeaseDuration { get; init; }
        = TimeSpan.FromSeconds(30);

    public TimeSpan HeartbeatInterval { get; init; }
        = TimeSpan.FromSeconds(10);

    public FailurePolicy FailurePolicy { get; init; }
        = FailurePolicy.Ignore;
}