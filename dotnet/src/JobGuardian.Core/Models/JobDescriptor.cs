using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Models;

/// <summary>
/// Describes a job registration managed by JobGuardian.
/// </summary>
/// <remarks>
/// A job descriptor binds a logical job key, the concrete job implementation type, and the policy
/// used for lease and heartbeat coordination.
/// </remarks>
public sealed record JobDescriptor(
    JobKey JobKey,
    Type JobType,
    JobExecutionPolicy Policy);