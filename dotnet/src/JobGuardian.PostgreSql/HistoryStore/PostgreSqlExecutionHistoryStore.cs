using Dapper;

using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.PostgreSql.Contracts;

namespace JobGuardian.PostgreSql.HistoryStore;

/// <summary>
/// Persists execution history entries in PostgreSQL.
/// </summary>
public sealed class PostgreSqlExecutionHistoryStore
    : IExecutionHistoryStore
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlExecutionHistoryStore"/> class.
    /// </summary>
    /// <param name="connectionFactory">
    /// Factory used to create PostgreSQL database connections.
    /// </param>
    public PostgreSqlExecutionHistoryStore(
        IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task CreateAsync(
        ExecutionHistoryEntry entry,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.CreateAsync(
                cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO jobguardian_execution_history
                (
                    execution_id,
                    tenant_id,
                    correlation_id,
                    conversation_id,
                    protocol_version,
                    schema_version,
                    job_namespace,
                    job_name,
                    application_name,
                    application_version,
                    environment,
                    cluster_name,
                    owner_id,
                    started_at_utc,
                    ended_at_utc,
                    outcome,
                    failure_category,
                    run_type,
                    triggered_by,
                    error_message,
                    execution_metadata
                )
                VALUES
                (
                    @ExecutionId,
                    @TenantId,
                    @CorrelationId,
                    @ConversationId,
                    @ProtocolVersion,
                    @SchemaVersion,
                    @JobNamespace,
                    @JobName,
                    @ApplicationName,
                    @ApplicationVersion,
                    @Environment,
                    @ClusterName,
                    @OwnerId,
                    @StartedAtUtc,
                    @EndedAtUtc,
                    @Outcome,
                    @FailureCategory,
                    @RunType,
                    @TriggeredBy,
                    @ErrorMessage,
                    CAST(@ExecutionMetadataJson AS JSONB)
                )
                """,
                CreateParameters(entry),
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        ExecutionHistoryEntry entry,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.CreateAsync(
                cancellationToken);

        var updatedRows =
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE jobguardian_execution_history
                    SET
                        tenant_id = @TenantId,
                        correlation_id = @CorrelationId,
                        conversation_id = @ConversationId,
                        protocol_version = @ProtocolVersion,
                        schema_version = @SchemaVersion,
                        job_namespace = @JobNamespace,
                        job_name = @JobName,
                        application_name = @ApplicationName,
                        application_version = @ApplicationVersion,
                        environment = @Environment,
                        cluster_name = @ClusterName,
                        owner_id = @OwnerId,
                        started_at_utc = @StartedAtUtc,
                        ended_at_utc = @EndedAtUtc,
                        outcome = @Outcome,
                        failure_category = @FailureCategory,
                        run_type = @RunType,
                        triggered_by = @TriggeredBy,
                        error_message = @ErrorMessage,
                        execution_metadata = CAST(@ExecutionMetadataJson AS JSONB)
                    WHERE execution_id = @ExecutionId
                    """,
                    CreateParameters(entry),
                    cancellationToken: cancellationToken));

        if (updatedRows == 0)
        {
            throw new KeyNotFoundException(
                $"Execution history entry '{entry.ExecutionId}' was not found.");
        }
    }

    /// <inheritdoc />
    public async Task<ExecutionHistoryEntry?> GetAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.CreateAsync(
                cancellationToken);

        var record =
            await connection.QuerySingleOrDefaultAsync<ExecutionHistoryRecord>(
                new CommandDefinition(
                    """
                    SELECT
                        execution_id AS ExecutionId,
                        tenant_id AS TenantId,
                        correlation_id AS CorrelationId,
                        conversation_id AS ConversationId,
                        protocol_version AS ProtocolVersion,
                        schema_version AS SchemaVersion,
                        job_namespace AS JobNamespace,
                        job_name AS JobName,
                        application_name AS ApplicationName,
                        application_version AS ApplicationVersion,
                        environment AS Environment,
                        cluster_name AS ClusterName,
                        owner_id AS OwnerId,
                        started_at_utc AS StartedAtUtc,
                        ended_at_utc AS EndedAtUtc,
                        outcome AS Outcome,
                        failure_category AS FailureCategory,
                        run_type AS RunType,
                        triggered_by AS TriggeredBy,
                        error_message AS ErrorMessage,
                        execution_metadata::TEXT AS ExecutionMetadataJson
                    FROM jobguardian_execution_history
                    WHERE execution_id = @ExecutionId
                    """,
                    new { ExecutionId = executionId },
                    cancellationToken: cancellationToken));

        return record?.ToEntry();
    }

    private static object CreateParameters(
        ExecutionHistoryEntry entry)
    {
        return new
        {
            entry.ExecutionId,
            entry.JobKey.TenantId,
            entry.CorrelationId,
            entry.ConversationId,
            entry.ProtocolVersion,
            entry.SchemaVersion,
            entry.JobKey.JobNamespace,
            entry.JobKey.JobName,
            entry.ApplicationName,
            entry.ApplicationVersion,
            entry.Environment,
            entry.ClusterName,
            entry.OwnerId,
            entry.StartedAtUtc,
            entry.EndedAtUtc,
            Outcome = entry.Outcome?.ToString(),
            entry.FailureCategory,
            RunType = entry.RunType.ToString(),
            TriggeredBy = entry.TriggeredBy.ToString(),
            entry.ErrorMessage,
            entry.ExecutionMetadataJson
        };
    }

    private sealed class ExecutionHistoryRecord
    {
        public Guid ExecutionId { get; set; }

        public string TenantId { get; set; } = string.Empty;

        public Guid? CorrelationId { get; set; }

        public Guid? ConversationId { get; set; }

        public string ProtocolVersion { get; set; } = string.Empty;

        public string SchemaVersion { get; set; } = string.Empty;

        public string JobNamespace { get; set; } = string.Empty;

        public string JobName { get; set; } = string.Empty;

        public string ApplicationName { get; set; } = string.Empty;

        public string? ApplicationVersion { get; set; }

        public string Environment { get; set; } = string.Empty;

        public string? ClusterName { get; set; }

        public string OwnerId { get; set; } = string.Empty;

        public DateTimeOffset StartedAtUtc { get; set; }

        public DateTimeOffset? EndedAtUtc { get; set; }

        public string? Outcome { get; set; }

        public string? FailureCategory { get; set; }

        public string RunType { get; set; } = string.Empty;

        public string TriggeredBy { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public string? ExecutionMetadataJson { get; set; }

        public ExecutionHistoryEntry ToEntry()
        {
            return new ExecutionHistoryEntry
            {
                ExecutionId = ExecutionId,
                JobKey = new JobKey(
                    TenantId,
                    JobNamespace,
                    JobName),
                CorrelationId = CorrelationId,
                ConversationId = ConversationId,
                ProtocolVersion = ProtocolVersion,
                SchemaVersion = SchemaVersion,
                ApplicationName = ApplicationName,
                ApplicationVersion = ApplicationVersion,
                Environment = Environment,
                ClusterName = ClusterName,
                OwnerId = OwnerId,
                StartedAtUtc = StartedAtUtc,
                EndedAtUtc = EndedAtUtc,
                Outcome = Outcome is null
                    ? null
                    : Enum.Parse<ExecutionOutcome>(Outcome),
                FailureCategory = FailureCategory,
                RunType = Enum.Parse<RunType>(RunType),
                TriggeredBy = Enum.Parse<TriggeredBy>(TriggeredBy),
                ErrorMessage = ErrorMessage,
                ExecutionMetadataJson = ExecutionMetadataJson
            };
        }
    }
}
