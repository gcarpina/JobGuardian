namespace JobGuardian.Abstractions.Models;

/// <summary>
/// Describes a running job execution that currently owns or is attempting to own a lease.
/// </summary>
public sealed record ActiveExecution
{
    /// <summary>
    /// Gets the logical identity of the job being executed.
    /// </summary>
    public required JobKey JobKey { get; init; }

    /// <summary>
    /// Gets the unique identifier for the execution attempt.
    /// </summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Gets the owner identifier for the runtime instance currently executing the job.
    /// </summary>
    public required string OwnerId { get; init; }
}