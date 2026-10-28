using Dapper;
using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;
using JobGuardian.PostgreSql.Contracts;

namespace JobGuardian.PostgreSql.AuditStore;

/// <summary>
/// Persists administrative audit entries in PostgreSQL.
/// </summary>
public sealed class PostgreSqlAuditStore
    : IAuditStore
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlAuditStore"/> class.
    /// </summary>
    /// <param name="connectionFactory">
    /// Factory used to create PostgreSQL database connections.
    /// </param>
    public PostgreSqlAuditStore(
        IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Writes an audit entry to the PostgreSQL audit log.
    /// </summary>
    /// <param name="entry">
    /// The audit entry to persist.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    public async Task WriteAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await using var connection =
            await _connectionFactory.CreateAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO jobguardian_audit_log
                (
                    audit_id,
                    tenant_id,
                    timestamp_utc,
                    user_id,
                    user_name,
                    action,
                    job_namespace,
                    job_name,
                    execution_id,
                    details
                )
                VALUES
                (
                    @AuditId,
                    @TenantId,
                    @TimestampUtc,
                    @UserId,
                    @UserName,
                    @Action,
                    @JobNamespace,
                    @JobName,
                    @ExecutionId,
                    CAST(@DetailsJson AS JSONB)
                )
                """,
                entry,
                cancellationToken: cancellationToken));
    }
}
