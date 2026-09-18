using Dapper;

using JobGuardian.Abstractions.Models;

using JobGuardian.PostgreSql.Connection;
using JobGuardian.PostgreSql.LeaseStore;
using JobGuardian.PostgreSql.Options;

using JobGuardian.PostgreSql.IntegrationTests.Infrastructure;

using Microsoft.Extensions.Options;

using Npgsql;

namespace JobGuardian.PostgreSql.IntegrationTests;

[Collection("postgresql")]
public sealed class ReleaseTests : PostgreSqlTestBase
{
    public ReleaseTests(
        PostgreSqlFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CT021_Release_Active_Lease_Should_Succeed()
    {
        await Fixture.ResetDatabaseAsync();

        // Arrange

        var leaseStore =
            CreateLeaseStore();

        var execution1 =
            CreateExecution(
                "owner-a");

        var execution2 =
            CreateExecution(
                "owner-b");

        var acquired =
            await leaseStore.TryAcquireAsync(
                execution1,
                LeaseDuration);

        Assert.True(
            acquired);

        // Act

        var released =
            await leaseStore.ReleaseAsync(
                execution1.JobKey,
                execution1.ExecutionId);

        // Assert

        Assert.True(
            released);

        var reacquired =
            await leaseStore.TryAcquireAsync(
                execution2,
                LeaseDuration);

        Assert.True(
            reacquired);

        await using var connection =
            new NpgsqlConnection(
                Fixture.ConnectionString);

        await connection.OpenAsync();

        var row =
            await connection.QuerySingleAsync<dynamic>(
                """
                SELECT
                    execution_id,
                    owner_id
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
            execution2.ExecutionId,
            (Guid)row.execution_id);

        Assert.Equal(
            execution2.OwnerId,
            (string)row.owner_id);
    }

    [Fact]
    public async Task CT022_Release_Lost_Lease_Should_Fail()
    {
        await Fixture.ResetDatabaseAsync();

        // Arrange

        var leaseStore =
            CreateLeaseStore();

        var execution1 =
            CreateExecution(
                "owner-a");

        var execution2 =
            CreateExecution(
                "owner-b");

        var firstAcquire =
            await leaseStore.TryAcquireAsync(
                execution1,
                LeaseDuration);

        Assert.True(
            firstAcquire);

        await using var connection =
            new NpgsqlConnection(
                Fixture.ConnectionString);

        await connection.OpenAsync();

        // Forza scadenza lease

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

        var secondAcquire =
            await leaseStore.TryAcquireAsync(
                execution2,
                LeaseDuration);

        Assert.True(
            secondAcquire);

        // Act

        var released =
            await leaseStore.ReleaseAsync(
                execution1.JobKey,
                execution1.ExecutionId);

        // Assert

        Assert.False(
            released);

        var row =
            await connection.QuerySingleAsync<dynamic>(
                """
                SELECT
                    execution_id,
                    owner_id
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
            execution2.ExecutionId,
            (Guid)row.execution_id);

        Assert.Equal(
            execution2.OwnerId,
            (string)row.owner_id);
    }
}