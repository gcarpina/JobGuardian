using JobGuardian.Abstractions.Contracts;

using JobGuardian.Core.DependencyInjection;

using JobGuardian.PostgreSql.Connection;
using JobGuardian.PostgreSql.LeaseStore;
using JobGuardian.PostgreSql.Options;
using JobGuardian.PostgreSql.State;
using JobGuardian.PostgreSql.DependencyInjection;
using JobGuardian.PostgreSql.Contracts;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace JobGuardian.PostgreSql.IntegrationTests.DependencyInjection;

public sealed class FluentRegistrationTests
{
    [Fact]
    public async Task UsePostgreSql_Should_Start_Host_With_Registered_Services()
    {
        // Arrange

        using var host =
            Host.CreateDefaultBuilder()
                .ConfigureServices(
                    services =>
                        services
                            .AddJobGuardian()
                            .UsePostgreSql(
                                "Host=localhost"))
                .Build();

        // Act

        await host.StartAsync();

        // Assert

        Assert.Single(
            host.Services.GetServices<IHostedService>());

        await host.StopAsync();
    }

    [Fact]
    public void CT1230_UsePostgreSql_Should_Register_LeaseStore()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act

        services
            .AddJobGuardian()
            .UsePostgreSql(
                "Host=localhost");

        var provider =
            services.BuildServiceProvider();

        // Assert

        var leaseStore =
            provider.GetService<ILeaseStore>();

        Assert.NotNull(
            leaseStore);

        Assert.IsType<PostgreSqlLeaseStore>(
            leaseStore);
    }

    [Fact]
    public void CT1240_UsePostgreSql_Should_Register_JobStateRepository()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act

        services
            .AddJobGuardian()
            .UsePostgreSql(
                "Host=localhost");

        var provider =
            services.BuildServiceProvider();

        // Assert

        var repository =
            provider.GetService<IJobStateRepository>();

        Assert.NotNull(
            repository);

        Assert.IsType<PostgreSqlJobStateRepository>(
            repository);
    }

    [Fact]
    public void CT1250_UsePostgreSql_Should_Register_ConnectionFactory()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act

        services
            .AddJobGuardian()
            .UsePostgreSql(
                "Host=localhost");

        var provider =
            services.BuildServiceProvider();

        // Assert

        Assert.NotNull(
            provider.GetService<IPostgreSqlConnectionFactory>());
    }

    [Fact]
    public void CT1260_UsePostgreSql_Should_Override_InMemory_StateRepository()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act

        services
            .AddJobGuardian()
            .UsePostgreSql(
                "Host=localhost");

        var provider =
            services.BuildServiceProvider();

        // Assert

        var repository =
            provider.GetRequiredService<IJobStateRepository>();

        Assert.IsType<PostgreSqlJobStateRepository>(
            repository);
    }

    [Fact]
    public void CT1270_UsePostgreSql_Should_Register_Options()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act

        services
            .AddJobGuardian()
            .UsePostgreSql(
                "Host=localhost");

        var provider =
            services.BuildServiceProvider();

        // Assert

        var options =
            provider.GetRequiredService<
                IOptions<PostgreSqlOptions>>();

        Assert.Equal(
            "Host=localhost",
            options.Value.ConnectionString);
    }

    [Fact]
    public void CT1280_UsePostgreSql_Should_Reject_Empty_ConnectionString()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act + Assert

        Assert.Throws<ArgumentException>(
            () =>
                services
                    .AddJobGuardian()
                    .UsePostgreSql(string.Empty));
    }
}