using Microsoft.Extensions.DependencyInjection;

namespace JobGuardian.Core.DependencyInjection;

public sealed class JobGuardianBuilder
{
    public IServiceCollection Services
    {
        get;
    }

    public JobGuardianBuilder(
        IServiceCollection services)
    {
        Services = services;
    }
}