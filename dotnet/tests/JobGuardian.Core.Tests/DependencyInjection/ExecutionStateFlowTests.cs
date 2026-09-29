using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.DependencyInjection;
using JobGuardian.Core.Hosting;
using JobGuardian.Core.Models;
using JobGuardian.Core.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JobGuardian.Core.Tests.DependencyInjection;

public sealed class ExecutionStateFlowTests
{
    [Fact]
    public async Task CT920_Failed_Job_Should_Be_Blocked_Until_Reset()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<ExecutionCounter>();
        services.AddJobGuardian();
        services.AddJob<FailOnceJob>(
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync"),
            new JobExecutionPolicy
            {
                LeaseDuration =
                    TimeSpan.FromMinutes(1),
                HeartbeatInterval =
                    TimeSpan.FromSeconds(20),
                FailurePolicy =
                    FailurePolicy.RequireManualReset
            });

        await using var provider =
            services.BuildServiceProvider();

        var hostedService =
            provider
                .GetServices<IHostedService>()
                .OfType<JobGuardianHostedService>()
                .Single();

        var stateManager =
            provider.GetRequiredService<IJobStateManager>();

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        await hostedService.ExecuteJobsAsync(
            CancellationToken.None);

        Assert.Equal(
            JobState.Blocked,
            await stateManager.GetCurrentStateAsync(
                jobKey));

        Assert.Equal(
            1,
            provider.GetRequiredService<ExecutionCounter>().Count);

        await hostedService.ExecuteJobsAsync(
            CancellationToken.None);

        Assert.Equal(
            1,
            provider.GetRequiredService<ExecutionCounter>().Count);

        await stateManager.ResetAsync(
            jobKey);

        await hostedService.ExecuteJobsAsync(
            CancellationToken.None);

        Assert.Equal(
            2,
            provider.GetRequiredService<ExecutionCounter>().Count);

        Assert.Equal(
            JobState.Eligible,
            await stateManager.GetCurrentStateAsync(
                jobKey));
    }

    [Fact]
    public async Task CT930_Failed_Job_With_Ignore_Policy_Should_Remain_Eligible_And_Run_Again()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<ExecutionCounter>();
        services.AddJobGuardian();
        services.AddJob<FailOnceJob>(
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync"),
            new JobExecutionPolicy
            {
                LeaseDuration =
                    TimeSpan.FromMinutes(1),
                HeartbeatInterval =
                    TimeSpan.FromSeconds(20),
                FailurePolicy =
                    FailurePolicy.Ignore
            });

        await using var provider =
            services.BuildServiceProvider();

        var hostedService =
            provider
                .GetServices<IHostedService>()
                .OfType<JobGuardianHostedService>()
                .Single();

        var stateManager =
            provider.GetRequiredService<IJobStateManager>();

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        await hostedService.ExecuteJobsAsync(
            CancellationToken.None);

        Assert.Equal(
            JobState.Eligible,
            await stateManager.GetCurrentStateAsync(
                jobKey));

        await hostedService.ExecuteJobsAsync(
            CancellationToken.None);

        Assert.Equal(
            2,
            provider.GetRequiredService<ExecutionCounter>().Count);
    }
}

public sealed class ExecutionCounter
{
    private int _count;

    public int Count
        => Volatile.Read(ref _count);

    public int Increment()
        => Interlocked.Increment(ref _count);
}

public sealed class FailOnceJob
    : IJob
{
    private readonly ExecutionCounter _counter;

    public FailOnceJob(
        ExecutionCounter counter)
    {
        _counter = counter;
    }

    public Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        if (_counter.Increment() == 1)
        {
            throw new InvalidOperationException(
                "First execution fails.");
        }

        return Task.CompletedTask;
    }
}
