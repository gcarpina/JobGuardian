using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.DependencyInjection;
using JobGuardian.Core.Models;
using JobGuardian.Core.Options;
using JobGuardian.Core.State;
using JobGuardian.PostgreSql.DependencyInjection;
using JobGuardian.PostgreSql.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JobGuardian.PostgreSql.IntegrationTests;

[Collection("postgresql")]
public sealed class ExecutionStateFlowTests
    : PostgreSqlTestBase
{
    public ExecutionStateFlowTests(
        PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task CT1050_Failed_Job_Should_Be_Persistently_Blocked_Until_Reset()
    {
        await Fixture.ResetDatabaseAsync();

        var probe =
            new ExecutionProbe();

        var jobKey =
            new JobKey(
                "default",
                "billing",
                "invoice-sync");

        using var host =
            Host.CreateDefaultBuilder()
                .ConfigureServices(
                    services =>
                    {
                        services.AddLogging();
                        services.AddSingleton(probe);

                        services
                            .AddJobGuardian()
                            .UsePostgreSql(
                                Fixture.ConnectionString);

                        services.AddSingleton(
                            new RuntimeOptions
                            {
                                PollingInterval =
                                    TimeSpan.FromMilliseconds(25)
                            });

                        services.AddJob<PostgreSqlFailOnceJob>(
                            jobKey,
                            new JobExecutionPolicy
                            {
                                LeaseDuration =
                                    TimeSpan.FromSeconds(10),
                                HeartbeatInterval =
                                    TimeSpan.FromSeconds(2),
                                FailurePolicy =
                                    FailurePolicy.RequireManualReset
                            });
                    })
                .Build();

        var stateManager =
            host.Services.GetRequiredService<IJobStateManager>();

        await host.StartAsync();

        try
        {
            await probe.FirstExecution.WaitAsync(
                TimeSpan.FromSeconds(10));

            await WaitForStateAsync(
                stateManager,
                jobKey,
                JobState.Blocked);

            await Task.Delay(
                TimeSpan.FromMilliseconds(150));

            Assert.Equal(
                1,
                probe.ExecutionCount);

            await stateManager.ResetAsync(
                jobKey);

            await probe.SecondExecution.WaitAsync(
                TimeSpan.FromSeconds(10));

            Assert.Equal(
                JobState.Eligible,
                await stateManager.GetCurrentStateAsync(
                    jobKey));
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static async Task WaitForStateAsync(
        IJobStateManager stateManager,
        JobKey jobKey,
        JobState expectedState)
    {
        using var timeout =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(10));

        while (await stateManager.GetCurrentStateAsync(
                   jobKey,
                   timeout.Token) != expectedState)
        {
            await Task.Delay(
                TimeSpan.FromMilliseconds(20),
                timeout.Token);
        }
    }
}

public sealed class ExecutionProbe
{
    private readonly TaskCompletionSource _firstExecution =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource _secondExecution =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _executionCount;

    public int ExecutionCount
        => Volatile.Read(ref _executionCount);

    public Task FirstExecution
        => _firstExecution.Task;

    public Task SecondExecution
        => _secondExecution.Task;

    public int RecordExecution()
    {
        var count =
            Interlocked.Increment(
                ref _executionCount);

        if (count == 1)
        {
            _firstExecution.TrySetResult();
        }
        else if (count == 2)
        {
            _secondExecution.TrySetResult();
        }

        return count;
    }
}

public sealed class PostgreSqlFailOnceJob
    : IJob
{
    private readonly ExecutionProbe _probe;

    public PostgreSqlFailOnceJob(
        ExecutionProbe probe)
    {
        _probe = probe;
    }

    public Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        if (_probe.RecordExecution() == 1)
        {
            throw new InvalidOperationException(
                "First execution fails.");
        }

        return Task.CompletedTask;
    }
}
