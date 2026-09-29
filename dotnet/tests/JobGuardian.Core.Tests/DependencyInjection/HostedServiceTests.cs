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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Core.State;

namespace JobGuardian.Core.Tests.DependencyInjection;

public sealed class HostedServiceTests
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
    public void CT240_HostedService_Should_Create_Scope_For_Registered_Jobs()
    {
        // Arrange

        var scopeFactory =
            Substitute.For<IServiceScopeFactory>();

        var scope =
            Substitute.For<IServiceScope>();

        var coordinator =
            Substitute.For<IJobExecutionCoordinator>();

        var logger =
            NullLogger<JobGuardianHostedService>
                .Instance;

        scopeFactory
            .CreateScope()
            .Returns(scope);

        var policy = TestDefaults.DefaultPolicy;

        var jobs =
            new[]
            {
            new JobDescriptor(
                new JobKey(
                    "tenant-a",
                    "finance",
                    "nightly-import"),
                typeof(DummyJob),
                policy)
            };

        var hostedService =
            new JobGuardianHostedService(
                jobs,
                scopeFactory,
                coordinator,
                Substitute.For<IJobStateManager>(),
                CreateIdentityProvider(),
                TestDefaults.DefaultRuntimeOptions,
                logger);

        // Act

        hostedService.DiscoverJobs();

        // Assert

        scopeFactory
            .Received(1)
            .CreateScope();
    }

    [Fact]
    public void CT250_HostedService_Should_Resolve_Registered_Job()
    {
        // Arrange

        var serviceProvider =
            Substitute.For<IServiceProvider>();

        serviceProvider
            .GetService(
                typeof(DummyJob))
            .Returns(
                new DummyJob());

        var scope =
            Substitute.For<IServiceScope>();

        scope.ServiceProvider
            .Returns(
                serviceProvider);

        var scopeFactory =
            Substitute.For<IServiceScopeFactory>();

        scopeFactory
            .CreateScope()
            .Returns(
                scope);

        var coordinator =
            Substitute.For<IJobExecutionCoordinator>();

        var policy = TestDefaults.DefaultPolicy;

        var logger =
            NullLogger<JobGuardianHostedService>
                .Instance;

        var jobs =
            new[]
            {
            new JobDescriptor(
                new JobKey(
                    "tenant-a",
                    "finance",
                    "nightly-import"),
                typeof(DummyJob),
                policy)
            };

        var hostedService =
            new JobGuardianHostedService(
                jobs,
                scopeFactory,
                coordinator,
                Substitute.For<IJobStateManager>(),
                CreateIdentityProvider(),
                TestDefaults.DefaultRuntimeOptions,
                logger);

        // Act

        hostedService.ResolveJobs();

        // Assert

        serviceProvider
            .Received(1)
            .GetService(
                typeof(DummyJob));
    }

    [Fact]
    public async Task CT270_HostedService_Should_Execute_Job_Through_Coordinator()
    {
        // Arrange

        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .Build();

        // Act

        await context.HostedService.ExecuteJobsAsync(
            CancellationToken.None);

        // Assert

        await context.Coordinator
            .Received(1)
            .ExecuteAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT280_HostedService_Should_Pass_Policy_To_Coordinator()
    {
        // Arrange

        JobExecutionOptions? capturedOptions =
            null;

        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .Build();

        context.Coordinator
            .ExecuteAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOptions =
                    callInfo.Arg<JobExecutionOptions>();

                return new ExecutionResult(
                    ExecutionOutcome.Succeeded);
            });

        // Act

        await context.HostedService.ExecuteJobsAsync(
            CancellationToken.None);

        // Assert

        Assert.NotNull(
            capturedOptions);

        Assert.Equal(
            TestDefaults.DefaultPolicy.LeaseDuration,
            capturedOptions!.LeaseDuration);

        Assert.Equal(
            TestDefaults.DefaultPolicy.HeartbeatInterval,
            capturedOptions.HeartbeatInterval);
    }

    [Fact]
    public async Task CT290_HostedService_Should_Pass_Execution_To_Coordinator()
    {
        // Arrange

        ActiveExecution? capturedExecution =
            null;

        var expectedJobKey =
            new JobKey(
                "tenant-a",
                "finance",
                nameof(DummyJob));

        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .Build();

        context.Coordinator
            .ExecuteAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedExecution =
                    callInfo.Arg<ActiveExecution>();

                return new ExecutionResult(
                    ExecutionOutcome.Succeeded);
            });

        // Act

        await context.HostedService.ExecuteJobsAsync(
            CancellationToken.None);

        // Assert

        Assert.NotNull(
            capturedExecution);

        Assert.Equal(
            expectedJobKey,
            capturedExecution!.JobKey);

        Assert.NotEqual(
            Guid.Empty,
            capturedExecution.ExecutionId);

        Assert.False(
            string.IsNullOrWhiteSpace(
                capturedExecution.OwnerId));
    }

    [Fact]
    public async Task CT300_HostedService_Should_Execute_Jobs_When_Started()
    {
        // Arrange

        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .Build();

        // Act

        await context.HostedService.StartAsync(
            CancellationToken.None);

        await Task.Delay(100);

        await context.HostedService.StopAsync(
            CancellationToken.None);

        // Assert

        await context.Coordinator
            .Received(1)
            .ExecuteAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT310_HostedService_Should_Execute_Jobs_Periodically()
    {
        // Arrange
        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .WithRuntimeOptions(
                    TestDefaults.FastRuntimeOptions)
                .Build();

        // Act

        await context.HostedService.StartAsync(
            CancellationToken.None);

        await Task.Delay(150);

        await context.HostedService.StopAsync(
            CancellationToken.None);

        // Assert

        var calls =
            context.Coordinator.ReceivedCalls()
                .Count(
                    x => x.GetMethodInfo().Name
                        == nameof(IJobExecutionCoordinator.ExecuteAsync));

        Assert.True(
            calls >= 2);
    }

    [Fact]
    public async Task CT320_HostedService_Should_Stop_Gracefully()
    {
        // Arrange

        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .Build();

        // Act

        await context.HostedService.StartAsync(
            CancellationToken.None);

        var exception =
            await Record.ExceptionAsync(
                () => context.HostedService.StopAsync(
                    CancellationToken.None));

        // Assert

        Assert.Null(
            exception);
    }

    [Fact]
    public async Task CT340_HostedService_Should_Continue_After_Job_Failure()
    {
        // Arrange
        var executionCount = 0;

        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .WithRuntimeOptions(
                    TestDefaults.FastRuntimeOptions)
                .Build();

        context.Coordinator
            .ExecuteAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                executionCount++;

                if (executionCount == 1)
                {
                    throw new InvalidOperationException(
                        "Boom");
                }

                return new ExecutionResult(
                    ExecutionOutcome.Succeeded);
            });

        // Act

        await context.HostedService.StartAsync(
            CancellationToken.None);

        await Task.Delay(150);

        await context.HostedService.StopAsync(
            CancellationToken.None);

        // Assert

        Assert.True(
            executionCount >= 2);
    }

    [Fact]
    public async Task CT350_HostedService_Should_Execute_All_Registered_Jobs()
    {
        // Arrange

        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .AddJob<SecondDummyJob>()
                .Build();

        // Act

        await context.HostedService.ExecuteJobsAsync(
            CancellationToken.None);

        // Assert

        await context.Coordinator
            .Received(2)
            .ExecuteAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT360_HostedService_Should_Use_Configured_Execution_Identity()
    {
        // Arrange

        ActiveExecution? capturedExecution =
            null;

        var context =
            new HostedServiceBuilder()
                .WithOwnerId(
                    "runtime-node-01")
                .AddJob<DummyJob>()
                .Build();

        context.Coordinator
            .ExecuteAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedExecution =
                    callInfo.Arg<ActiveExecution>();

                return new ExecutionResult(
                    ExecutionOutcome.Succeeded);
            });

        // Act

        await context.HostedService.ExecuteJobsAsync(
            CancellationToken.None);

        // Assert

        Assert.NotNull(
            capturedExecution);

        Assert.Equal(
            "runtime-node-01",
            capturedExecution!.OwnerId);
    }

    [Fact]
    public async Task CT370_HostedService_Should_Log_Job_Failures()
    {
        // Arrange

        var logger =
            new TestLogger<JobGuardianHostedService>();

        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .WithLogger(logger)
                .Build();

        context.Coordinator
            .ExecuteAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<ExecutionResult>(
                    new InvalidOperationException(
                        "Boom")));

        // Act

        await context.HostedService.StartAsync(
            CancellationToken.None);

        await Task.Delay(150);

        await context.HostedService.StopAsync(
            CancellationToken.None);

        // Assert

        Assert.Contains(
            LogLevel.Error,
            logger.Levels);
    }

    [Fact]
    public async Task CT380_HostedService_Should_Log_Service_Start()
    {
        // Arrange

        var logger =
            new TestLogger<JobGuardianHostedService>();

        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .WithLogger(logger)
                .Build();

        // Act

        await context.HostedService.StartAsync(
            CancellationToken.None);

        await Task.Delay(50);

        await context.HostedService.StopAsync(
            CancellationToken.None);

        // Assert

        Assert.Contains(
            LogLevel.Information,
            logger.Levels);
    }

    [Fact]
    public async Task CT390_HostedService_Should_Log_Service_Stop()
    {
        // Arrange

        var logger =
            new TestLogger<JobGuardianHostedService>();

        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .WithLogger(logger)
                .Build();

        // Act

        await context.HostedService.StartAsync(
            CancellationToken.None);

        await Task.Delay(50);

        await context.HostedService.StopAsync(
            CancellationToken.None);

        // Assert

        Assert.Contains(
            logger.Messages,
            x => x.Contains(
                "stopped",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CT900_Blocked_Jobs_Should_Not_Be_Executed()
    {
        // Arrange

        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .WithJobState(
                    JobState.Blocked)
                .Build();

        // Act

        await context.HostedService.ExecuteJobsAsync(
            CancellationToken.None);

        // Assert

        await context.Coordinator
            .DidNotReceive()
            .ExecuteAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT910_Eligible_Jobs_Should_Be_Executed()
    {
        // Arrange

        var context =
            new HostedServiceBuilder()
                .AddJob<DummyJob>()
                .WithJobState(
                    JobState.Eligible)
                .Build();

        // Act

        await context.HostedService.ExecuteJobsAsync(
            CancellationToken.None);

        // Assert

        await context.Coordinator
            .Received(1)
            .ExecuteAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT940_HostedService_Should_Apply_Execution_Result_Using_Job_Failure_Policy()
    {
        var executionResult =
            new ExecutionResult(
                ExecutionOutcome.Failed,
                new InvalidOperationException("Job failed"));

        var context =
            new HostedServiceBuilder()
                .WithFailurePolicy(
                    FailurePolicy.RequireManualReset)
                .AddJob<DummyJob>()
                .Build();

        context.Coordinator
            .ExecuteAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(executionResult));

        await context.HostedService.ExecuteJobsAsync(
            CancellationToken.None);

        await context.StateManager
            .Received(1)
            .HandleExecutionResultAsync(
                Arg.Is<JobKey>(
                    key => key.JobName == nameof(DummyJob)),
                FailurePolicy.RequireManualReset,
                executionResult,
                Arg.Any<CancellationToken>());
    }
}