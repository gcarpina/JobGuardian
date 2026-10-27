using JobGuardian.Core.DependencyInjection;
using JobGuardian.Core.Hosting;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using JobGuardian.Core.Models;
using JobGuardian.Core.Tests.TestKit;

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

    [Theory]
    [InlineData(30, 30)]
    [InlineData(0, 5)]
    [InlineData(30, 0)]
    public void CT019_AddJob_Should_Reject_Invalid_Lease_And_Heartbeat_Settings(
        int leaseSeconds,
        int heartbeatSeconds)
    {
        var services =
            new ServiceCollection();

        Assert.ThrowsAny<ArgumentException>(
            () => services.AddJob<DummyJob>(
                new JobGuardian.Abstractions.Models.JobKey(
                    "tenant-a",
                    "tests",
                    "invalid-lease"),
                new JobExecutionPolicy
                {
                    LeaseDuration = TimeSpan.FromSeconds(leaseSeconds),
                    HeartbeatInterval = TimeSpan.FromSeconds(heartbeatSeconds)
                }));
    }
}