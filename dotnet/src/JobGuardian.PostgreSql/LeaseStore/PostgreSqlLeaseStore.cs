using Dapper;

using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;

using JobGuardian.PostgreSql.Contracts;
using JobGuardian.PostgreSql.Models.Persistence;

namespace JobGuardian.PostgreSql.LeaseStore;

public sealed class PostgreSqlLeaseStore
    : ILeaseStore
{
    private readonly IPostgreSqlConnectionFactory
        _connectionFactory;

    public PostgreSqlLeaseStore(
        IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory =
            connectionFactory;
    }

    public async Task<bool> TryAcquireAsync(
    ActiveExecution execution,
    TimeSpan leaseDuration,
    CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.CreateAsync(
                cancellationToken);

        const string sql =
            """
            INSERT INTO jobguardian_active_executions
            (
                tenant_id,
                job_namespace,
                job_name,
                execution_id,
                owner_id,
                acquired_at_utc,
                renewed_at_utc,
                lease_until_utc
            )
            VALUES
            (
                @TenantId,
                @JobNamespace,
                @JobName,
                @ExecutionId,
                @OwnerId,
                CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP + @LeaseDuration
            )
            ON CONFLICT
            (
                tenant_id,
                job_namespace,
                job_name
            )
            DO UPDATE
            SET
                execution_id = EXCLUDED.execution_id,
                owner_id = EXCLUDED.owner_id,
                acquired_at_utc = CURRENT_TIMESTAMP,
                renewed_at_utc = CURRENT_TIMESTAMP,
                lease_until_utc = CURRENT_TIMESTAMP + @LeaseDuration
            WHERE
                jobguardian_active_executions.lease_until_utc
                    < CURRENT_TIMESTAMP;
            """;

        var command =
            new CommandDefinition(
                sql,
                new
                {
                    execution.JobKey.TenantId,
                    execution.JobKey.JobNamespace,
                    execution.JobKey.JobName,
                    execution.ExecutionId,
                    execution.OwnerId,
                    LeaseDuration = leaseDuration
                },
                cancellationToken: cancellationToken);

        var affectedRows =
            await connection.ExecuteAsync(command);

        return affectedRows == 1;
    }

    public async Task<bool> RenewAsync(
    JobKey jobKey,
    Guid executionId,
    TimeSpan leaseDuration,
    CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            UPDATE jobguardian_active_executions
            SET
                renewed_at_utc = CURRENT_TIMESTAMP,
                lease_until_utc = CURRENT_TIMESTAMP + @LeaseDuration
            WHERE
                tenant_id = @TenantId
                AND job_namespace = @JobNamespace
                AND job_name = @JobName
                AND execution_id = @ExecutionId
                AND lease_until_utc >= CURRENT_TIMESTAMP
            """;

        await using var connection =
            await _connectionFactory.CreateAsync(
                cancellationToken);

        var affectedRows =
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        TenantId = jobKey.TenantId,
                        JobNamespace = jobKey.JobNamespace,
                        JobName = jobKey.JobName,
                        ExecutionId = executionId,
                        LeaseDuration = leaseDuration
                    },
                    cancellationToken: cancellationToken));

        return affectedRows == 1;
    }

    public async Task<bool> ReleaseAsync(
    JobKey jobKey,
    Guid executionId,
    CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            DELETE FROM jobguardian_active_executions
            WHERE
                tenant_id = @TenantId
                AND job_namespace = @JobNamespace
                AND job_name = @JobName
                AND execution_id = @ExecutionId
            """;

        await using var connection =
            await _connectionFactory.CreateAsync(
                cancellationToken);

        var affectedRows =
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        TenantId = jobKey.TenantId,
                        JobNamespace = jobKey.JobNamespace,
                        JobName = jobKey.JobName,
                        ExecutionId = executionId
                    },
                    cancellationToken: cancellationToken));

        return affectedRows == 1;
    }

    public async Task<ActiveLease?> GetActiveLeaseAsync(
    JobKey jobKey,
    CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            SELECT
                execution_id AS ExecutionId,
                owner_id AS OwnerId,
                acquired_at_utc AS AcquiredAtUtc,
                renewed_at_utc AS RenewedAtUtc,
                lease_until_utc AS LeaseUntilUtc
            FROM jobguardian_active_executions
            WHERE
                tenant_id = @TenantId
                AND job_namespace = @JobNamespace
                AND job_name = @JobName
                AND lease_until_utc >= CURRENT_TIMESTAMP
            """;

        await using var connection =
            await _connectionFactory.CreateAsync(
                cancellationToken);

        var record =
            await connection.QuerySingleOrDefaultAsync<ActiveLeaseRecord>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        TenantId = jobKey.TenantId,
                        JobNamespace = jobKey.JobNamespace,
                        JobName = jobKey.JobName
                    },
                    cancellationToken: cancellationToken));

        if (record is null)
        {
            return null;
        }

        return new ActiveLease
        {
            JobKey = jobKey,
            ExecutionId = record.ExecutionId,
            OwnerId = record.OwnerId,
            AcquiredAtUtc = record.AcquiredAtUtc,
            RenewedAtUtc = record.RenewedAtUtc,
            LeaseUntilUtc = record.LeaseUntilUtc
        };
    }
}