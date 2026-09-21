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

/// <summary>
/// Configures JobGuardian to use PostgreSQL for lease coordination and job state persistence.
/// </summary>
public static class JobGuardianBuilderExtensions
{
    /// <summary>
    /// Configures JobGuardian to use a PostgreSQL connection string for persistence.
    /// </summary>
    /// <param name="builder">
    /// The JobGuardian builder being configured.
    /// </param>
    /// <param name="connectionString">
    /// The PostgreSQL connection string used by the persistence layer.
    /// </param>
    /// <returns>
    /// The same builder so configuration calls can be chained.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the connection string is empty or whitespace.
    /// </exception>
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

    /// <summary>
    /// Configures JobGuardian to use the supplied PostgreSQL options.
    /// </summary>
    /// <param name="builder">
    /// The JobGuardian builder being configured.
    /// </param>
    /// <param name="options">
    /// The PostgreSQL options used for connection and storage setup.
    /// </param>
    /// <returns>
    /// The same builder so additional configuration can be chained.
    /// </returns>
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