namespace JobGuardian.Abstractions.Models;

public sealed record AuditEntry
{
    public required Guid AuditId { get; init; }

    public required string TenantId { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    public required string UserId { get; init; }

    public required string UserName { get; init; }

    public required string Action { get; init; }

    public string? JobNamespace { get; init; }

    public string? JobName { get; init; }

    public Guid? ExecutionId { get; init; }

    public string? DetailsJson { get; init; }
}