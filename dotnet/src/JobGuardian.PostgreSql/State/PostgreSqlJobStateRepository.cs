using Dapper;

using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

using JobGuardian.Abstractions.Contracts;

using JobGuardian.PostgreSql.Connection;

namespace JobGuardian.PostgreSql.State;

using JobGuardian.PostgreSql.Contracts;

/// <summary>
/// Persists JobGuardian job state in PostgreSQL.
/// </summary>
public sealed class PostgreSqlJobStateRepository
    : IJobStateRepository
{
    private readonly IPostgreSqlConnectionFactory
        _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlJobStateRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">
    /// Factory used to create PostgreSQL database connections.
    /// </param>
    public PostgreSqlJobStateRepository(
        IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory =
            connectionFactory;
    }

    /// <summary>
    /// Gets the current state for the specified job.
    /// </summary>
    /// <param name="jobKey">
    /// The logical identity of the job whose state is requested.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The current job state, or <c>null</c> when no state is persisted.
    /// </returns>
    public async Task<JobState?> GetAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory
                .CreateAsync(
                    cancellationToken);

        var value =
            await connection.QuerySingleOrDefaultAsync<string>(
                """
                SELECT state
                FROM jobguardian_job_state
                WHERE
                    tenant_id = @TenantId
                    AND job_namespace = @JobNamespace
                    AND job_name = @JobName
                """,
                new
                {
                    jobKey.TenantId,
                    jobKey.JobNamespace,
                    jobKey.JobName
                });

        if (value is null)
        {
            return null;
        }

        return Enum.Parse<JobState>(
            value);
    }

    /// <summary>
    /// Persists the state for the specified job.
    /// </summary>
    /// <param name="jobKey">
    /// The logical identity of the job whose state should be written.
    /// </param>
    /// <param name="state">
    /// The state to persist.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    public async Task SetAsync(
        JobKey jobKey,
        JobState state,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory
                .CreateAsync(
                    cancellationToken);

        if (state == JobState.Eligible)
        {
            await connection.ExecuteAsync(
                """
                DELETE
                FROM jobguardian_job_state
                WHERE
                    tenant_id = @TenantId
                    AND job_namespace = @JobNamespace
                    AND job_name = @JobName
                """,
                new
                {
                    jobKey.TenantId,
                    jobKey.JobNamespace,
                    jobKey.JobName
                });

            return;
        }

        await connection.ExecuteAsync(
            """
            INSERT INTO jobguardian_job_state
            (
                tenant_id,
                job_namespace,
                job_name,
                state,
                updated_at_utc
            )
            VALUES
            (
                @TenantId,
                @JobNamespace,
                @JobName,
                @State,
                CURRENT_TIMESTAMP
            )
            ON CONFLICT
            (
                tenant_id,
                job_namespace,
                job_name
            )
            DO UPDATE
            SET
                state = EXCLUDED.state,
                updated_at_utc = EXCLUDED.updated_at_utc
            """,
            new
            {
                jobKey.TenantId,
                jobKey.JobNamespace,
                jobKey.JobName,
                State = state.ToString()
            });
    }
}