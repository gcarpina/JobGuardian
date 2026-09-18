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
public sealed class RenewTests : PostgreSqlTestBase
{
    public RenewTests(
        PostgreSqlFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CT016_Renew_Active_Lease_Should_Succeed()
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

        var before =
            await connection.QuerySingleAsync<dynamic>(
                """
                SELECT
                    renewed_at_utc,
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

        // Let some time pass to make the change visible

        await Task.Delay(
            TimeSpan.FromSeconds(1));

        // Act

        var renewed =
            await leaseStore.RenewAsync(
                execution.JobKey,
                execution.ExecutionId,
                LeaseDuration);

        // Assert

        Assert.True(
            renewed);

        var after =
            await connection.QuerySingleAsync<dynamic>(
                """
                SELECT
                    renewed_at_utc,
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

        Assert.True(
            ((DateTimeOffset)after.renewed_at_utc)
            >
            ((DateTimeOffset)before.renewed_at_utc));

        Assert.True(
            ((DateTimeOffset)after.lease_until_utc)
            >
            ((DateTimeOffset)before.lease_until_utc));
    }

    [Fact]
    public async Task CT017_Renew_Lost_Lease_Should_Fail()
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

        // Forza la scadenza della lease

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

        // Owner B acquisisce la lease

        var secondAcquire =
            await leaseStore.TryAcquireAsync(
                execution2,
                LeaseDuration);

        Assert.True(
            secondAcquire);

        // Act

        var renewed =
            await leaseStore.RenewAsync(
                execution1.JobKey,
                execution1.ExecutionId,
                LeaseDuration);

        // Assert

        Assert.False(
            renewed);

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