using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Abstractions.Models;

/// <summary>
/// Captures the operational history and result of a single job execution attempt.
/// </summary>
public sealed record ExecutionHistoryEntry
{
    /// <summary>
    /// Gets the unique execution identifier.
    /// </summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Gets the logical job associated with the execution.
    /// </summary>
    public required JobKey JobKey { get; init; }

    /// <summary>
    /// Gets the correlation identifier, when available.
    /// </summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>
    /// Gets the conversation identifier, when available.
    /// </summary>
    public Guid? ConversationId { get; init; }

    /// <summary>
    /// Gets the protocol version used by the execution metadata.
    /// </summary>
    public required string ProtocolVersion { get; init; }

    /// <summary>
    /// Gets the schema version used by the execution metadata.
    /// </summary>
    public required string SchemaVersion { get; init; }

    /// <summary>
    /// Gets the application name that owns the execution.
    /// </summary>
    public required string ApplicationName { get; init; }

    /// <summary>
    /// Gets the application version, when known.
    /// </summary>
    public string? ApplicationVersion { get; init; }

    /// <summary>
    /// Gets the environment in which the execution ran.
    /// </summary>
    public required string Environment { get; init; }

    /// <summary>
    /// Gets the cluster name, when available.
    /// </summary>
    public string? ClusterName { get; init; }

    /// <summary>
    /// Gets the owner identifier that executed the job.
    /// </summary>
    public required string OwnerId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the execution started.
    /// </summary>
    public required DateTimeOffset StartedAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the execution ended, when available.
    /// </summary>
    public DateTimeOffset? EndedAtUtc { get; init; }

    /// <summary>
    /// Gets the final execution outcome, when available.
    /// </summary>
    public ExecutionOutcome? Outcome { get; init; }

    /// <summary>
    /// Gets the failure category, when the execution failed.
    /// </summary>
    public string? FailureCategory { get; init; }

    /// <summary>
    /// Gets the run type that initiated the execution.
    /// </summary>
    public required RunType RunType { get; init; }

    /// <summary>
    /// Gets the actor or source that triggered the execution.
    /// </summary>
    public required TriggeredBy TriggeredBy { get; init; }

    /// <summary>
    /// Gets the error message associated with the execution, when applicable.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets any additional execution metadata serialized as JSON.
    /// </summary>
    public string? ExecutionMetadataJson { get; init; }
}