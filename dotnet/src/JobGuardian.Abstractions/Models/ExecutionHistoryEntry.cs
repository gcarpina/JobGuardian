using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Abstractions.Models;

public sealed record ExecutionHistoryEntry
{
    public required Guid ExecutionId { get; init; }

    public required JobKey JobKey { get; init; }

    public Guid? CorrelationId { get; init; }

    public Guid? ConversationId { get; init; }

    public required string ProtocolVersion { get; init; }

    public required string SchemaVersion { get; init; }

    public required string ApplicationName { get; init; }

    public string? ApplicationVersion { get; init; }

    public required string Environment { get; init; }

    public string? ClusterName { get; init; }

    public required string OwnerId { get; init; }

    public required DateTimeOffset StartedAtUtc { get; init; }

    public DateTimeOffset? EndedAtUtc { get; init; }

    public ExecutionOutcome? Outcome { get; init; }

    public string? FailureCategory { get; init; }

    public required RunType RunType { get; init; }

    public required TriggeredBy TriggeredBy { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ExecutionMetadataJson { get; init; }
}