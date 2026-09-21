using Microsoft.Extensions.DependencyInjection;

namespace JobGuardian.Core.DependencyInjection;

/// <summary>
/// Provides a fluent configuration container for JobGuardian services.
/// </summary>
/// <remarks>
/// This builder is used to register JobGuardian components and attach job definitions to the
/// dependency injection container.
/// </remarks>
public sealed class JobGuardianBuilder
{
    /// <summary>
    /// Gets the dependency injection collection configured for JobGuardian.
    /// </summary>
    public IServiceCollection Services
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobGuardianBuilder"/> class.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    public JobGuardianBuilder(
        IServiceCollection services)
    {
        Services = services;
    }
}