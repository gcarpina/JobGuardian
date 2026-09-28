using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.DependencyInjection;
using JobGuardian.Core.Execution;
using JobGuardian.Core.Hosting;
using JobGuardian.Core.Models;
using JobGuardian.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using JobGuardian.Core.Tests.TestKit;

namespace JobGuardian.Core.Tests.DependencyInjection;

public sealed class DependencyInjectionTests
{
    private static IExecutionIdentityProvider CreateIdentityProvider()
    {
        var provider =
            Substitute.For<IExecutionIdentityProvider>();

        provider
            .GetOwnerId()
            .Returns("test-node");

        return provider;
    }

    [Fact]
    public void CT200_AddJobGuardian_Should_Register_Hosted_Service()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act

        services.AddJobGuardian();

        // Assert

        var registration =
            services.SingleOrDefault(
                x =>
                    x.ServiceType ==
                    typeof(IHostedService));

        Assert.NotNull(
            registration);
    }

    [Fact]
    public async Task AddJobGuardian_Should_Start_Host_With_Default_Configuration()
    {
        // Arrange

        using var host =
            Host.CreateDefaultBuilder()
                .ConfigureServices(
                    services => services.AddJobGuardian())
                .Build();

        // Act

        await host.StartAsync();

        // Assert

        Assert.Single(
            host.Services.GetServices<IHostedService>());

        await host.StopAsync();
    }

    [Fact]
    public void CT210_AddJob_Should_Register_Job_Type()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act

        var jobKey =
            new JobKey(
                "tenant-a",
                "finance",
                "nightly-import");

        var policy = TestDefaults.DefaultPolicy;

        services.AddJob<DummyJob>(
            jobKey,
            policy);

        var provider =
            services.BuildServiceProvider();

        // Assert

        var job =
            provider.GetService<DummyJob>();

        Assert.NotNull(
            job);
    }

    [Fact]
    public void CT220_AddJob_Should_Register_Job_Metadata()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act

        var jobKey =
                    new JobKey(
                        "tenant-a",
                        "finance",
                        "nightly-import");

        var policy = TestDefaults.DefaultPolicy;

        services.AddJob<DummyJob>(
            jobKey,
            policy);

        var provider =
            services.BuildServiceProvider();

        // Assert

        var descriptors =
            provider.GetServices<JobDescriptor>();

        Assert.Contains(
            descriptors,
            x =>
                x.JobType == typeof(DummyJob)
                && x.JobKey == jobKey);
    }

    [Fact]
    public void CT230_HostedService_Should_Receive_Registered_Jobs()
    {
        // Arrange

        var services =
            new ServiceCollection();

        services.AddJobGuardian();

        var jobKey =
            new JobKey(
                "tenant-a",
                "finance",
                "nightly-import");

        var policy = TestDefaults.DefaultPolicy;

        services.AddJob<DummyJob>(
            jobKey,
            policy);

        services.AddSingleton<IJobExecutionCoordinator>(
            Substitute.For<IJobExecutionCoordinator>());

        services.AddLogging();

        // Act

        var hostedService =
            ActivatorUtilities.CreateInstance<JobGuardianHostedService>(
                services.BuildServiceProvider());

        // Assert

        Assert.Contains(
            hostedService.Jobs,
            x =>
                x.JobKey == jobKey &&
                x.JobType == typeof(DummyJob));
    }

    [Fact]
    public void CT275_AddJob_Should_Register_Execution_Policy()
    {
        // Arrange

        var services =
            new ServiceCollection();

        var jobKey =
            new JobKey(
                "tenant-a",
                "finance",
                "nightly-import");

        var policy = TestDefaults.DefaultPolicy;

        // Act

        services.AddJob<DummyJob>(
            jobKey,
            policy);

        var provider =
            services.BuildServiceProvider();

        // Assert

        var descriptor =
            provider.GetRequiredService<JobDescriptor>();

        Assert.Equal(
            policy,
            descriptor.Policy);
    }
}