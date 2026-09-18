using JobGuardian.Core.DependencyInjection;
using JobGuardian.Core.Hosting;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JobGuardian.Core.Tests.DependencyInjection;

public sealed class FluentRegistrationTests
{
    [Fact]
    public void CT1200_AddJobGuardian_Should_Return_JobGuardianBuilder()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act

        var builder =
            services.AddJobGuardian();

        // Assert

        Assert.NotNull(
            builder);

        Assert.IsType<JobGuardianBuilder>(
            builder);
    }

    [Fact]
    public void CT1210_AddJobGuardian_Should_Still_Register_Hosted_Service()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act

        services.AddJobGuardian();

        // Assert

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType
                == typeof(IHostedService));
    }

    [Fact]
    public void CT1220_AddJobGuardian_Should_Return_Builder_With_Same_ServiceCollection()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act

        var builder =
            services.AddJobGuardian();

        services.AddSingleton<object>();

        // Assert

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType
                == typeof(object));
    }
}