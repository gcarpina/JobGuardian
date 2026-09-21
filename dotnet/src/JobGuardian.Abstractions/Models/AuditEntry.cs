namespace JobGuardian.Abstractions.Models;

/// <summary>
/// Represents an audit record describing an operator or system action related to distributed job coordination.
/// </summary>
public sealed record AuditEntry
{
    /// <summary>
    /// Gets the unique identifier of the audit record.
    /// </summary>
    public required Guid AuditId { get; init; }

    /// <summary>
    /// Gets the tenant associated with the action.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the action occurred.
    /// </summary>
    public required DateTimeOffset TimestampUtc { get; init; }

    /// <summary>
    /// Gets the identifier of the user or service principal that performed the action.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Gets the display name associated with the actor.
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// Gets the action that was performed.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the namespace of the related job, when available.
    /// </summary>
    public string? JobNamespace { get; init; }

    /// <summary>
    /// Gets the name of the related job, when available.
    /// </summary>
    public string? JobName { get; init; }

    /// <summary>
    /// Gets the execution identifier associated with the action, when available.
    /// </summary>
    public Guid? ExecutionId { get; init; }

    /// <summary>
    /// Gets the serialized details associated with the action.
    /// </summary>
    public string? DetailsJson { get; init; }
}