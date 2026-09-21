using Dapper;

using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;

using JobGuardian.PostgreSql.Contracts;
using JobGuardian.PostgreSql.Models.Persistence;

namespace JobGuardian.PostgreSql.LeaseStore;

/// <summary>
/// Stores job leases in PostgreSQL and enforces distributed ownership coordination.
/// </summary>
public sealed class PostgreSqlLeaseStore
    : ILeaseStore
{
    private readonly IPostgreSqlConnectionFactory
        _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlLeaseStore"/> class.
    /// </summary>
    /// <param name="connectionFactory">
    /// Factory used to create database connections for lease operations.
    /// </param>
    public PostgreSqlLeaseStore(
        IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory =
            connectionFactory;
    }

    /// <summary>
    /// Attempts to acquire a distributed lease for the specified execution.
    /// </summary>
    /// <param name="execution">
    /// The execution that is requesting lease ownership.
    /// </param>
    /// <param name="leaseDuration">
    /// The duration for which the lease should remain valid.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// <c>true</c> when the lease is acquired; otherwise, <c>false</c>.
    /// </returns>
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

    /// <summary>
    /// Renews an existing lease for the specified execution.
    /// </summary>
    /// <param name="jobKey">
    /// The job whose lease should be renewed.
    /// </param>
    /// <param name="executionId">
    /// The execution identifier carrying the active lease.
    /// </param>
    /// <param name="leaseDuration">
    /// The duration to extend the lease by.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// <c>true</c> when the lease is renewed; otherwise, <c>false</c>.
    /// </returns>
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

    /// <summary>
    /// Releases the lease for the specified execution.
    /// </summary>
    /// <param name="jobKey">
    /// The job whose lease should be released.
    /// </param>
    /// <param name="executionId">
    /// The execution identifier that owns the lease.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// <c>true</c> when the lease is released; otherwise, <c>false</c>.
    /// </returns>
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

    /// <summary>
    /// Gets the active lease for the specified job when one is currently valid.
    /// </summary>
    /// <param name="jobKey">
    /// The job whose active lease is requested.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The active lease, or <c>null</c> when no valid lease exists.
    /// </returns>
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