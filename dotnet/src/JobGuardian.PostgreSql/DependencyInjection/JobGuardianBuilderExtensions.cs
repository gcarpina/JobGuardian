using JobGuardian.Abstractions.Contracts;

using JobGuardian.Core.DependencyInjection;

using JobGuardian.PostgreSql.Connection;
using JobGuardian.PostgreSql.Contracts;
using JobGuardian.PostgreSql.LeaseStore;
using JobGuardian.PostgreSql.Options;
using JobGuardian.PostgreSql.State;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JobGuardian.PostgreSql.DependencyInjection;

public static class JobGuardianBuilderExtensions
{
    public static JobGuardianBuilder UsePostgreSql(
        this JobGuardianBuilder builder,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(
            connectionString))
        {
            throw new ArgumentException(
                "Connection string cannot be empty.",
                nameof(connectionString));
        }

        return builder.UsePostgreSql(
            new PostgreSqlOptions
            {
                ConnectionString =
                    connectionString
            });
    }

    public static JobGuardianBuilder UsePostgreSql(
        this JobGuardianBuilder builder,
        PostgreSqlOptions options)
    {
        builder.Services.AddSingleton(
            Microsoft.Extensions.Options.Options.Create(options));

        builder.Services.AddSingleton<
            IPostgreSqlConnectionFactory,
            PostgreSqlConnectionFactory>();

        builder.Services.AddSingleton<
            ILeaseStore,
            PostgreSqlLeaseStore>();

        builder.Services.AddSingleton<
            IJobStateRepository,
            PostgreSqlJobStateRepository>();

        return builder;
    }
}