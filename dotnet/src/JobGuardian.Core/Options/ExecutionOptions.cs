using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Options;

/// <summary>
/// Defines the operational configuration for a JobGuardian execution request.
/// </summary>
public sealed record ExecutionOptions
{
    /// <summary>
    /// Gets the logical identifier of the job being executed.
    /// </summary>
    public required JobKey JobKey { get; init; }

    /// <summary>
    /// Gets the application name associated with the execution.
    /// </summary>
    public required string ApplicationName { get; init; }

    /// <summary>
    /// Gets the environment in which the execution is running.
    /// </summary>
    public required string Environment { get; init; }

    /// <summary>
    /// Gets the owner identity for the execution.
    /// </summary>
    public required OwnerId OwnerId { get; init; }

    /// <summary>
    /// Gets or sets the lease duration for the execution.
    /// </summary>
    public TimeSpan LeaseDuration { get; init; }
        = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the interval used to maintain the lease while the job runs.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; init; }
        = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the policy used when the execution fails.
    /// </summary>
    public FailurePolicy FailurePolicy { get; init; }
        = FailurePolicy.Ignore;
}