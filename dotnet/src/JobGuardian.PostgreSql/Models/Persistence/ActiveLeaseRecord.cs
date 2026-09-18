namespace JobGuardian.PostgreSql.Models.Persistence;

internal sealed class ActiveLeaseRecord
{
    public Guid ExecutionId { get; init; }

    public string OwnerId { get; init; } = string.Empty;

    public DateTimeOffset AcquiredAtUtc { get; init; }

    public DateTimeOffset RenewedAtUtc { get; init; }

    public DateTimeOffset LeaseUntilUtc { get; init; }
}