namespace JobGuardian.Abstractions.Models;

/// <summary>
/// Identifies a job within a tenant, namespace, and name.
/// </summary>
/// <remarks>
/// JobGuardian uses a job key as the canonical identity for state, lease ownership, and execution
/// coordination across application instances.
/// </remarks>
/// <param name="TenantId">The tenant that owns the job identity.</param>
/// <param name="JobNamespace">The namespace containing the job.</param>
/// <param name="JobName">The job name within the namespace.</param>
public sealed record JobKey(
    string TenantId,
    string JobNamespace,
    string JobName);