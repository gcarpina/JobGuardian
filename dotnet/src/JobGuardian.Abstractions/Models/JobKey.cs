namespace JobGuardian.Abstractions.Models;

public sealed record JobKey(
    string TenantId,
    string JobNamespace,
    string JobName);