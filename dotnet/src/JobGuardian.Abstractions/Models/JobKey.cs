namespace JobGuardian.Abstractions.Models;

/// <summary>
/// Identifies a job within a tenant, namespace, and name.
/// </summary>
/// <remarks>
/// JobGuardian uses a job key as the canonical identity for state, lease ownership, and execution
/// coordination across application instances.
/// </remarks>
public sealed record JobKey(
    string TenantId,
    string JobNamespace,
    string JobName);