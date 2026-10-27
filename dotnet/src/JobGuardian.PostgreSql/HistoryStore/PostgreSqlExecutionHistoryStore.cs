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

    /// <summary>
    /// Creates an execution history record in PostgreSQL.
    /// </summary>
    /// <param name="entry">The history record to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>A task that represents the asynchronous insert operation.</returns>
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
                    lease_acquired_at_utc,
                    ended_at_utc,
                    outcome,
                    outcome_reason,
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
                    @LeaseAcquiredAtUtc,
                    @EndedAtUtc,
                    @Outcome,
                    @OutcomeReason,
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

    /// <summary>
    /// Updates an existing execution history record in PostgreSQL.
    /// </summary>
    /// <param name="entry">The history record containing the updated values.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    /// <exception cref="KeyNotFoundException">
    /// The record does not exist in the execution history table.
    /// </exception>
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
                        lease_acquired_at_utc = COALESCE(@LeaseAcquiredAtUtc, lease_acquired_at_utc),
                        ended_at_utc = @EndedAtUtc,
                        outcome = @Outcome,
                        outcome_reason = @OutcomeReason,
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

    /// <summary>
    /// Retrieves an execution history record by its execution identifier.
    /// </summary>
    /// <param name="executionId">The unique identifier of the execution.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>The matching record, or <see langword="null"/> when it does not exist.</returns>
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
                        lease_acquired_at_utc AS LeaseAcquiredAtUtc,
                        ended_at_utc AS EndedAtUtc,
                        outcome AS Outcome,
                        outcome_reason AS OutcomeReason,
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

    /// <summary>
    /// Queries a page of execution history for a tenant-scoped job.
    /// </summary>
    /// <param name="query">The job identity, time bounds, filters, and paging settings.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>The requested page and total count of matching records.</returns>
    /// <exception cref="ArgumentException">
    /// The job identity, time range, or paging settings are invalid.
    /// </exception>
    public async Task<ExecutionHistoryPage> QueryAsync(
        ExecutionHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateQuery(query);

        await using var connection =
            await _connectionFactory.CreateAsync(
                cancellationToken);

        var parameters = new
        {
            query.JobKey.TenantId,
            query.JobKey.JobNamespace,
            query.JobKey.JobName,
            query.StartedFromUtc,
            query.StartedBeforeUtc,
            Outcome = query.Outcome?.ToString(),
            Offset = checked((query.Page - 1) * query.PageSize),
            query.PageSize
        };

        const string whereClause =
            """
            WHERE tenant_id = @TenantId
                AND job_namespace = @JobNamespace
                AND job_name = @JobName
                AND started_at_utc >= @StartedFromUtc
                AND started_at_utc < @StartedBeforeUtc
                AND (
                    CAST(@Outcome AS VARCHAR) IS NULL
                    OR outcome = CAST(@Outcome AS VARCHAR)
                )
            """;

        var totalCount =
            await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    $"SELECT COUNT(*) FROM jobguardian_execution_history {whereClause}",
                    parameters,
                    cancellationToken: cancellationToken));

        var records =
            await connection.QueryAsync<ExecutionHistoryRecord>(
                new CommandDefinition(
                    $"""
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
                        lease_acquired_at_utc AS LeaseAcquiredAtUtc,
                        ended_at_utc AS EndedAtUtc,
                        outcome AS Outcome,
                        outcome_reason AS OutcomeReason,
                        failure_category AS FailureCategory,
                        run_type AS RunType,
                        triggered_by AS TriggeredBy,
                        error_message AS ErrorMessage,
                        execution_metadata::TEXT AS ExecutionMetadataJson
                    FROM jobguardian_execution_history
                    {whereClause}
                    ORDER BY started_at_utc DESC, execution_id DESC
                    OFFSET @Offset
                    LIMIT @PageSize
                    """,
                    parameters,
                    cancellationToken: cancellationToken));

        return new ExecutionHistoryPage(
            records.Select(record => record.ToEntry()).ToArray(),
            query.Page,
            query.PageSize,
            totalCount);
    }

    /// <summary>
    /// Records the database time at which an execution acquired its lease.
    /// </summary>
    /// <param name="executionId">The execution identifier to mark.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>The lease-acquisition timestamp according to PostgreSQL.</returns>
    /// <exception cref="KeyNotFoundException">
    /// The execution history record does not exist.
    /// </exception>
    public async Task<DateTimeOffset> MarkLeaseAcquiredAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.CreateAsync(
                cancellationToken);

        var acquiredAtUtc =
            await connection.ExecuteScalarAsync<DateTime?>(
                new CommandDefinition(
                    """
                    UPDATE jobguardian_execution_history
                    SET lease_acquired_at_utc =
                        COALESCE(lease_acquired_at_utc, CURRENT_TIMESTAMP)
                    WHERE execution_id = @ExecutionId
                        AND outcome IS NULL
                    RETURNING lease_acquired_at_utc
                    """,
                    new { ExecutionId = executionId },
                    cancellationToken: cancellationToken));

        if (acquiredAtUtc is null)
        {
            throw new KeyNotFoundException(
                $"Incomplete execution history entry '{executionId}' was not found.");
        }

        return new DateTimeOffset(
            DateTime.SpecifyKind(
                acquiredAtUtc.Value,
                DateTimeKind.Utc));
    }

    /// <summary>
    /// Marks prior lease-acquired executions without a terminal outcome as abandoned.
    /// </summary>
    /// <remarks>
    /// This operation is intended to run after the current execution has acquired the same job's
    /// lease. It does not establish that a previous process has terminated.
    /// </remarks>
    /// <param name="jobKey">The tenant-scoped identity of the job being recovered.</param>
    /// <param name="currentExecutionId">The newly acquired execution to exclude.</param>
    /// <param name="endedAtUtc">The timestamp to persist as the abandoned outcome time.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>The number of prior execution records marked as abandoned.</returns>
    public async Task<int> MarkPreviousIncompleteAsAbandonedAsync(
        JobKey jobKey,
        Guid currentExecutionId,
        DateTimeOffset endedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobKey);

        await using var connection =
            await _connectionFactory.CreateAsync(
                cancellationToken);

        return await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE jobguardian_execution_history
                SET
                    ended_at_utc = @EndedAtUtc,
                    outcome = @Outcome,
                    outcome_reason = @OutcomeReason,
                    failure_category = @FailureCategory
                WHERE tenant_id = @TenantId
                    AND job_namespace = @JobNamespace
                    AND job_name = @JobName
                    AND execution_id <> @CurrentExecutionId
                    AND lease_acquired_at_utc IS NOT NULL
                    AND outcome IS NULL
                """,
                new
                {
                    jobKey.TenantId,
                    jobKey.JobNamespace,
                    jobKey.JobName,
                    CurrentExecutionId = currentExecutionId,
                    EndedAtUtc = endedAtUtc,
                    Outcome = ExecutionOutcome.Abandoned.ToString(),
                    OutcomeReason =
                        ExecutionOutcomeReason.LeaseExpiredWithoutOutcome.ToString(),
                    FailureCategory = "LeaseExpiredWithoutOutcome"
                },
                cancellationToken: cancellationToken));
    }

    private static void ValidateQuery(
        ExecutionHistoryQuery query)
    {
        ArgumentNullException.ThrowIfNull(query.JobKey);

        if (string.IsNullOrWhiteSpace(query.JobKey.TenantId)
            || string.IsNullOrWhiteSpace(query.JobKey.JobNamespace)
            || string.IsNullOrWhiteSpace(query.JobKey.JobName))
        {
            throw new ArgumentException(
                "The history query requires a complete tenant-scoped job key.",
                nameof(query));
        }

        if (query.StartedFromUtc >= query.StartedBeforeUtc)
        {
            throw new ArgumentException(
                "StartedFromUtc must be earlier than StartedBeforeUtc.",
                nameof(query));
        }

        if (query.Page < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.Page),
                "Page must be at least 1.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.PageSize),
                "PageSize must be between 1 and 100.");
        }

        _ = checked((query.Page - 1) * query.PageSize);
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
            entry.LeaseAcquiredAtUtc,
            entry.EndedAtUtc,
            Outcome = entry.Outcome?.ToString(),
            OutcomeReason = entry.OutcomeReason?.ToString(),
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

        public DateTimeOffset? LeaseAcquiredAtUtc { get; set; }

        public DateTimeOffset? EndedAtUtc { get; set; }

        public string? Outcome { get; set; }

        public string? OutcomeReason { get; set; }

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
                LeaseAcquiredAtUtc = LeaseAcquiredAtUtc,
                EndedAtUtc = EndedAtUtc,
                Outcome = Outcome is null
                    ? null
                    : Enum.Parse<ExecutionOutcome>(Outcome),
                OutcomeReason = OutcomeReason is null
                    ? null
                    : Enum.Parse<ExecutionOutcomeReason>(OutcomeReason),
                FailureCategory = FailureCategory,
                RunType = Enum.Parse<RunType>(RunType),
                TriggeredBy = Enum.Parse<TriggeredBy>(TriggeredBy),
                ErrorMessage = ErrorMessage,
                ExecutionMetadataJson = ExecutionMetadataJson
            };
        }
    }
}
