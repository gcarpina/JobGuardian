using JobGuardian.Abstractions.Contracts;

using JobGuardian.PostgreSql.Connection;
using JobGuardian.PostgreSql.Contracts;
using JobGuardian.PostgreSql.HistoryStore;
using JobGuardian.PostgreSql.LeaseStore;
using JobGuardian.PostgreSql.Options;
using JobGuardian.PostgreSql.State;

using Microsoft.Extensions.DependencyInjection;

namespace JobGuardian.PostgreSql.DependencyInjection;

/// <summary>
/// Configures PostgreSQL persistence for JobGuardian using a standard service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures JobGuardian to use PostgreSQL persistence using the supplied options callback.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <param name="configure">
    /// A callback used to configure the PostgreSQL options.
    /// </param>
    /// <returns>
    /// The same service collection so additional registrations can be chained.
    /// </returns>
    public static IServiceCollection UsePostgreSql(
        this IServiceCollection services,
        Action<PostgreSqlOptions> configure)
    {
        services.Configure(
            configure);

        services.AddSingleton<
            IPostgreSqlConnectionFactory,
            PostgreSqlConnectionFactory>();

        services.AddSingleton<
            ILeaseStore,
            PostgreSqlLeaseStore>();

        services.AddSingleton<
            IJobStateRepository,
            PostgreSqlJobStateRepository>();

        services.AddSingleton<
            IExecutionHistoryStore,
            PostgreSqlExecutionHistoryStore>();

        return services;
    }
}