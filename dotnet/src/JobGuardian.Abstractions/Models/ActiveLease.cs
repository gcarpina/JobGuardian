namespace JobGuardian.Abstractions.Models;

public sealed record ActiveLease
{
    public required JobKey JobKey { get; init; }

    public required Guid ExecutionId { get; init; }

    public required string OwnerId { get; init; }

    public required DateTimeOffset AcquiredAtUtc { get; init; }

    public required DateTimeOffset RenewedAtUtc { get; init; }

    public required DateTimeOffset LeaseUntilUtc { get; init; }
}