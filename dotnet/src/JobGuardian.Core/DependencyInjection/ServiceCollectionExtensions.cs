using JobGuardian.Core.Hosting;
using Microsoft.Extensions.DependencyInjection;
using JobGuardian.Abstractions.Contracts;
using JobGuardian.Core.Models;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Execution;
using JobGuardian.Core.Options;
using JobGuardian.Core.Identity;
using JobGuardian.Core.Policies;
using JobGuardian.Core.State;

namespace JobGuardian.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static JobGuardianBuilder AddJobGuardian(
        this IServiceCollection services)
    {
        services.AddSingleton(
            new RuntimeOptions
            {
                PollingInterval =
                    TimeSpan.FromSeconds(30)
            });

        services.AddHostedService<JobGuardianHostedService>();

        services.AddSingleton<
            IExecutionIdentityProvider,
            MachineNameExecutionIdentityProvider>();

        services.AddSingleton<
            IJobExecutionCoordinator,
            JobExecutionCoordinator>();

        services.AddSingleton<
            IJobStateManager,
            JobStateManager>();

        services.AddSingleton<
            IJobStateTransitionEngine,
            JobStateTransitionEngine>();

        services.AddSingleton<
            IJobStateRepository,
            InMemoryJobStateRepository>();

        services.AddSingleton<
            IFailurePolicyEvaluator,
            FailurePolicyEvaluator>();

        return new JobGuardianBuilder(
            services);
    }


    public static IServiceCollection AddJob<TJob>(
        this IServiceCollection services,
        JobKey jobKey,
        JobExecutionPolicy policy)
        where TJob : class, IJob
    {
        services.AddTransient<TJob>();

        services.AddSingleton(
            new JobDescriptor(
                jobKey,
                typeof(TJob),
                policy));

        return services;
    }
}