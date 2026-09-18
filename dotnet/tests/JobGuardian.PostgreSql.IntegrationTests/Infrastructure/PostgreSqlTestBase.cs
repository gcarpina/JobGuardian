using JobGuardian.Abstractions.Models;

using JobGuardian.PostgreSql.Connection;
using JobGuardian.PostgreSql.LeaseStore;
using JobGuardian.PostgreSql.Options;
using JobGuardian.PostgreSql.State;

namespace JobGuardian.PostgreSql.IntegrationTests.Infrastructure;

public abstract class PostgreSqlTestBase
{
    protected const string TenantId = "default";

    protected const string JobNamespace = "finance";

    protected const string JobName = "nightly-import";

    protected static readonly TimeSpan LeaseDuration =
        TimeSpan.FromSeconds(30);

    protected readonly PostgreSqlFixture Fixture;

    protected PostgreSqlTestBase(
        PostgreSqlFixture fixture)
    {
        Fixture = fixture;
    }

    protected PostgreSqlLeaseStore CreateLeaseStore()
    {
        var postgresOptions =
            Microsoft.Extensions.Options.Options.Create(
                new PostgreSqlOptions
                {
                    ConnectionString =
                        Fixture.ConnectionString
                });

        var connectionFactory =
            new PostgreSqlConnectionFactory(
                postgresOptions);

        return new PostgreSqlLeaseStore(
            connectionFactory);
    }

    protected static ActiveExecution CreateExecution(
        string ownerId)
    {
        return new ActiveExecution
        {
            JobKey =
                new JobKey(
                    TenantId,
                    JobNamespace,
                    JobName),

            ExecutionId =
                Guid.NewGuid(),

            OwnerId =
                ownerId
        };
    }

    protected PostgreSqlJobStateRepository CreateJobStateRepository()
    {
        var postgresOptions =
            Microsoft.Extensions.Options.Options.Create(
                new PostgreSqlOptions
                {
                    ConnectionString =
                        Fixture.ConnectionString
                });

        var connectionFactory =
            new PostgreSqlConnectionFactory(
                postgresOptions);

        return new PostgreSqlJobStateRepository(
            connectionFactory);
    }
}