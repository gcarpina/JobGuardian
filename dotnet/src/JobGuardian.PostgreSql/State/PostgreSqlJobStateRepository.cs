using Dapper;

using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

using JobGuardian.Abstractions.Contracts;

using JobGuardian.PostgreSql.Connection;

namespace JobGuardian.PostgreSql.State;

using JobGuardian.PostgreSql.Contracts;

public sealed class PostgreSqlJobStateRepository
    : IJobStateRepository
{
    private readonly IPostgreSqlConnectionFactory
        _connectionFactory;

    public PostgreSqlJobStateRepository(
        IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory =
            connectionFactory;
    }

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