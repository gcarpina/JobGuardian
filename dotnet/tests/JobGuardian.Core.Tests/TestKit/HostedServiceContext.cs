using JobGuardian.Abstractions.Contracts;
using JobGuardian.Core.Execution;
using JobGuardian.Core.Hosting;
using Microsoft.Extensions.DependencyInjection;
using JobGuardian.Core.State;

internal sealed class HostedServiceContext
{
    public required JobGuardianHostedService
        HostedService
    {
        get;
        init;
    }

    public required IJobExecutionCoordinator
        Coordinator
    {
        get;
        init;
    }

    public required IJobStateManager StateManager
    {
        get;
        init;
    }

    public required IServiceProvider
        ServiceProvider
    {
        get;
        init;
    }

    public required IServiceScopeFactory
        ScopeFactory
    {
        get;
        init;
    }

    public required IExecutionIdentityProvider
        IdentityProvider
    {
        get;
        init;
    }

    public required IServiceScope
        Scope
    {
        get;
        init;
    }
}