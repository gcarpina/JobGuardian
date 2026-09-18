using Dapper;
using JobGuardian.Abstractions.Models;
using JobGuardian.PostgreSql.IntegrationTests.Infrastructure;

using Npgsql;

namespace JobGuardian.PostgreSql.IntegrationTests;

[Collection("postgresql")]
public sealed class LeaseQueryTests
    : PostgreSqlTestBase
{
    public LeaseQueryTests(
        PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task CT031_Get_Existing_Lease_Should_Return_Lease()
    {
        await Fixture.ResetDatabaseAsync();

        // Arrange

        var leaseStore =
            CreateLeaseStore();

        var execution =
            CreateExecution(
                "owner-a");

        var acquired =
            await leaseStore.TryAcquireAsync(
                execution,
                LeaseDuration);

        Assert.True(
            acquired);

        // Act

        var lease =
            await leaseStore.GetActiveLeaseAsync(
                execution.JobKey);

        // Assert

        Assert.NotNull(
            lease);

        Assert.Equal(
            execution.ExecutionId,
            lease!.ExecutionId);

        Assert.Equal(
            execution.OwnerId,
            lease.OwnerId);

        // Verify also the persisted record

        await using var connection =
            new NpgsqlConnection(
                Fixture.ConnectionString);

        await connection.OpenAsync();

        var row =
            await connection.QuerySingleAsync<dynamic>(
                """
                SELECT
                    execution_id,
                    owner_id,
                    lease_until_utc
                FROM jobguardian_active_executions
                WHERE
                    tenant_id = @TenantId
                    AND job_namespace = @JobNamespace
                    AND job_name = @JobName
                """,
                new
                {
                    TenantId,
                    JobNamespace,
                    JobName
                });

        Assert.Equal(
            execution.ExecutionId,
            (Guid)row.execution_id);

        Assert.Equal(
            execution.OwnerId,
            (string)row.owner_id);

        Assert.NotNull(
            row.lease_until_utc);
    }

    [Fact]
    public async Task CT032_Get_Missing_Lease_Should_Return_Null()
    {
        await Fixture.ResetDatabaseAsync();

        // Arrange

        var leaseStore =
            CreateLeaseStore();

        var jobKey =
            new JobKey(
                TenantId,
                JobNamespace,
                JobName);

        // Act

        var lease =
            await leaseStore.GetActiveLeaseAsync(
                jobKey);

        // Assert

        Assert.Null(
            lease);
    }

    [Fact]
    public async Task CT033_Get_Expired_Lease_Should_Return_Null()
    {
        await Fixture.ResetDatabaseAsync();

        // Arrange

        var leaseStore =
            CreateLeaseStore();

        var execution =
            CreateExecution(
                "owner-a");

        var acquired =
            await leaseStore.TryAcquireAsync(
                execution,
                LeaseDuration);

        Assert.True(
            acquired);

        await using var connection =
            new NpgsqlConnection(
                Fixture.ConnectionString);

        await connection.OpenAsync();

        await connection.ExecuteAsync(
            """
            UPDATE jobguardian_active_executions
            SET lease_until_utc =
                CURRENT_TIMESTAMP - INTERVAL '1 minute'
            WHERE
                tenant_id = @TenantId
                AND job_namespace = @JobNamespace
                AND job_name = @JobName
            """,
            new
            {
                TenantId,
                JobNamespace,
                JobName
            });

        // Act

        var lease =
            await leaseStore.GetActiveLeaseAsync(
                execution.JobKey);

        // Assert

        Assert.Null(
            lease);
    }
}