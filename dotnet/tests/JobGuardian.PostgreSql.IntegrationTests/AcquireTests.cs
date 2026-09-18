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
public sealed class AcquireTests : PostgreSqlTestBase
{
    public AcquireTests(
        PostgreSqlFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CT001_Acquire_Free_Job_Should_Succeed()
    {
        await Fixture.ResetDatabaseAsync();

        // Arrange

        var leaseStore =
            CreateLeaseStore();

        var execution =
            CreateExecution(
                "test-owner");

        // Act

        var acquired =
            await leaseStore.TryAcquireAsync(
                execution,
                LeaseDuration);

        // Assert

        Assert.True(acquired);

        await using var connection =
            new NpgsqlConnection(
                Fixture.ConnectionString);

        await connection.OpenAsync();

        var count =
            await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)
                FROM jobguardian_active_executions
                """);

        Assert.Equal(
            1,
            count);

        var row =
            await connection.QuerySingleAsync<dynamic>(
                """
                SELECT
                    tenant_id,
                    job_namespace,
                    job_name,
                    execution_id,
                    owner_id,
                    acquired_at_utc,
                    renewed_at_utc,
                    lease_until_utc
                FROM jobguardian_active_executions
                """);

        Assert.Equal(
            execution.JobKey.TenantId,
            (string)row.tenant_id);

        Assert.Equal(
            execution.JobKey.JobNamespace,
            (string)row.job_namespace);

        Assert.Equal(
            execution.JobKey.JobName,
            (string)row.job_name);

        Assert.Equal(
            execution.OwnerId,
            (string)row.owner_id);

        Assert.Equal(
            execution.ExecutionId,
            (Guid)row.execution_id);

        Assert.Equal(
            (DateTimeOffset)row.acquired_at_utc,
            (DateTimeOffset)row.renewed_at_utc);

        Assert.NotNull(
            row.acquired_at_utc);

        Assert.NotNull(
            row.renewed_at_utc);

        Assert.NotNull(
            row.lease_until_utc);

        Assert.True(
            ((DateTimeOffset)row.lease_until_utc)
            >
            ((DateTimeOffset)row.acquired_at_utc));
    }

    [Fact]
    public async Task CT002_Acquire_Busy_Job_Should_Fail()
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

        // Act

        var firstAcquire =
            await leaseStore.TryAcquireAsync(
                execution1,
                LeaseDuration);

        var secondAcquire =
            await leaseStore.TryAcquireAsync(
                execution2,
                LeaseDuration);

        // Assert

        Assert.True(
            firstAcquire);

        Assert.False(
            secondAcquire);

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
            execution1.ExecutionId,
            (Guid)row.execution_id);

        Assert.Equal(
            execution1.OwnerId,
            (string)row.owner_id);
    }

    [Fact]
    public async Task CT004_Acquire_Expired_Lease_Should_Succeed()
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

        // Acquire iniziale

        var firstAcquire =
            await leaseStore.TryAcquireAsync(
                execution1,
                LeaseDuration);

        Assert.True(
            firstAcquire);

        // Simula lease scaduta

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

        var secondAcquire =
            await leaseStore.TryAcquireAsync(
                execution2,
                LeaseDuration);

        // Assert

        Assert.True(
            secondAcquire);

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
    public async Task CT005_Concurrent_Acquisition_Should_Produce_A_Single_Winner()
    {
        await Fixture.ResetDatabaseAsync();

        // Arrange

        var leaseStore =
            CreateLeaseStore();

        const int contenders = 10;

        var tasks =
            Enumerable.Range(1, contenders)
                .Select(index =>
                {
                    var execution =
                        CreateExecution(
                            $"owner-{index}");

                    return leaseStore.TryAcquireAsync(
                        execution,
                        LeaseDuration);
                });

        // Act

        var results =
            await Task.WhenAll(
                tasks);

        // Assert

        var successes =
            results.Count(x => x);

        var failures =
            results.Count(x => !x);

        Assert.Equal(
            1,
            successes);

        Assert.Equal(
            9,
            failures);

        await using var connection =
            new NpgsqlConnection(
                Fixture.ConnectionString);

        await connection.OpenAsync();

        var count =
            await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)
                FROM jobguardian_active_executions
                """);

        Assert.Equal(
            1,
            count);

        var winner =
            await connection.QuerySingleAsync<dynamic>(
                """
                SELECT
                    execution_id,
                    owner_id
                FROM jobguardian_active_executions
                """);

        Assert.NotEqual(
            Guid.Empty,
            (Guid)winner.execution_id);

        Assert.False(
            string.IsNullOrWhiteSpace(
                (string)winner.owner_id));
    }
}