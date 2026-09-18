using JobGuardian.Abstractions.Contracts;

using JobGuardian.PostgreSql.Connection;
using JobGuardian.PostgreSql.Contracts;
using JobGuardian.PostgreSql.LeaseStore;
using JobGuardian.PostgreSql.Options;
using JobGuardian.PostgreSql.State;

using Microsoft.Extensions.DependencyInjection;

namespace JobGuardian.PostgreSql.DependencyInjection;

public static class ServiceCollectionExtensions
{
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

        return services;
    }
}