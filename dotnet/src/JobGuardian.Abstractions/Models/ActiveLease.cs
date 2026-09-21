namespace JobGuardian.Abstractions.Models;

/// <summary>
/// Represents the currently active lease held by a job execution.
/// </summary>
public sealed record ActiveLease
{
    /// <summary>
    /// Gets the logical identity of the job associated with the lease.
    /// </summary>
    public required JobKey JobKey { get; init; }

    /// <summary>
    /// Gets the unique identifier of the execution that currently owns the lease.
    /// </summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Gets the identifier of the owning runtime instance.
    /// </summary>
    public required string OwnerId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the lease was first acquired.
    /// </summary>
    public required DateTimeOffset AcquiredAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the lease was most recently renewed.
    /// </summary>
    public required DateTimeOffset RenewedAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the lease expires and ownership is no longer valid.
    /// </summary>
    public required DateTimeOffset LeaseUntilUtc { get; init; }
}