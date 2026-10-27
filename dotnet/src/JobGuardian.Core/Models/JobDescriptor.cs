using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Models;

/// <summary>
/// Describes a job registration managed by JobGuardian.
/// </summary>
/// <remarks>
/// A job descriptor binds a logical job key, the concrete job implementation type, and the policy
/// used for lease and heartbeat coordination.
/// </remarks>
/// <param name="JobKey">The tenant-scoped identity of the registered job.</param>
/// <param name="JobType">The concrete type that implements the job.</param>
/// <param name="Policy">The execution and failure policy for the job.</param>
public sealed record JobDescriptor(
    JobKey JobKey,
    Type JobType,
    JobExecutionPolicy Policy);