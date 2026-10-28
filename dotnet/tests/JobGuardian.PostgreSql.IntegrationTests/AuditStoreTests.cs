using JobGuardian.Abstractions.Models;
using JobGuardian.PostgreSql.IntegrationTests.Infrastructure;
using Npgsql;

namespace JobGuardian.PostgreSql.IntegrationTests;

[Collection("postgresql")]
public sealed class AuditStoreTests
    : PostgreSqlTestBase
{
    public AuditStoreTests(
        PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task CT1410_WriteAuditEntry_Should_Persist_Reset_Request_With_ActorIdOnly()
    {
        await Fixture.ResetDatabaseAsync();

        var entry =
            new AuditEntry
            {
                AuditId = Guid.NewGuid(),
                TenantId = TenantId,
                TimestampUtc = DateTimeOffset.UtcNow,
                UserId = "operator-42",
                Action = "ResetJobRequested",
                JobNamespace = JobNamespace,
                JobName = JobName
            };

        await CreateAuditStore().WriteAsync(entry);

        await using var connection =
            new NpgsqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command =
            new NpgsqlCommand(
                """
                SELECT
                    tenant_id,
                    timestamp_utc,
                    user_id,
                    user_name,
                    action,
                    job_namespace,
                    job_name
                FROM jobguardian_audit_log
                WHERE audit_id = @AuditId
                """,
                connection);
        command.Parameters.AddWithValue(
            "AuditId",
            entry.AuditId);

        await using var reader =
            await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal(entry.TenantId, reader.GetString(0));
        Assert.True(
            (entry.TimestampUtc - reader.GetFieldValue<DateTimeOffset>(1)).Duration()
            < TimeSpan.FromMicroseconds(1));
        Assert.Equal(entry.UserId, reader.GetString(2));
        Assert.True(reader.IsDBNull(3));
        Assert.Equal(entry.Action, reader.GetString(4));
        Assert.Equal(entry.JobNamespace, reader.GetString(5));
        Assert.Equal(entry.JobName, reader.GetString(6));
    }
}
