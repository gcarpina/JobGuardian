using JobGuardian.Abstractions.Models;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Core.Execution;
using JobGuardian.Core.Models;
using JobGuardian.Core.Tests.Infrastructure;
using NSubstitute;

namespace JobGuardian.Core.Tests.Execution;

public sealed class JobExecutionCoordinatorTests
    : CoreTestBase
{
    [Fact]
    public async Task CT101_Execute_When_Lease_Acquired_Should_Run_Job()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var heartbeatService =
            CreateHeartbeatService();

        heartbeatService
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);

        var execution =
            TestData.CreateExecution(
                "owner-a");

        var jobExecuted = false;

        var options =
            TestData.CreateOptions();

        // Act

        var result =
            await coordinator.ExecuteAsync(
                execution,
                options,
                _ =>
                {
                    jobExecuted = true;

                    return Task.CompletedTask;
                });

        // Assert

        Assert.Equal(
            ExecutionOutcome.Succeeded,
            result.Outcome);

        Assert.True(
            jobExecuted);
    }

    [Fact]
    public async Task CT102_Execute_When_Lease_Not_Acquired_Should_Skip_Job()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var heartbeatService =
            CreateHeartbeatService();

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);

        var execution =
            TestData.CreateExecution(
                "owner-a");

        var jobExecuted = false;

        var options =
            TestData.CreateOptions();

        // Act

        var result =
            await coordinator.ExecuteAsync(
                execution,
                options,
                _ =>
                {
                    jobExecuted = true;

                    return Task.CompletedTask;
                });

        // Assert

        Assert.Equal(
            ExecutionOutcome.Skipped,
            result.Outcome);

        Assert.False(
            jobExecuted);

        await leaseStore
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<JobKey>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT103_Execute_Should_Release_Lease_When_Job_Completes()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var heartbeatService =
            CreateHeartbeatService();

        heartbeatService
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);

        var execution =
            TestData.CreateExecution(
                "owner-a");

        var options =
            TestData.CreateOptions();

        // Act

        await coordinator.ExecuteAsync(
            execution,
            options,
            _ => Task.CompletedTask);

        // Assert

        await leaseStore
            .Received(1)
            .ReleaseAsync(
                execution.JobKey,
                execution.ExecutionId,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT155_Execute_When_Lease_Release_Fails_Should_Return_LeaseLost()
    {
        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        leaseStore
            .ReleaseAsync(
                Arg.Any<JobKey>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var heartbeatService =
            CreateHeartbeatService();

        heartbeatService
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);

        var result =
            await coordinator.ExecuteAsync(
                TestData.CreateExecution("owner-a"),
                TestData.CreateOptions(),
                _ => Task.CompletedTask);

        Assert.Equal(
            ExecutionOutcome.LeaseLost,
            result.Outcome);
    }

    [Fact]
    public async Task CT104_Execute_Should_Release_Lease_When_Job_Fails()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var heartbeatService =
            CreateHeartbeatService();

        heartbeatService
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);

        var execution =
            TestData.CreateExecution(
                "owner-a");

        var options =
            TestData.CreateOptions();

        // Act

        var result =
            await coordinator.ExecuteAsync(
                execution,
                options,
                _ =>
                    throw new InvalidOperationException(
                        "Test exception"));

        // Assert

        Assert.Equal(
            ExecutionOutcome.Failed,
            result.Outcome);

        Assert.IsType<InvalidOperationException>(
            result.Exception);

        await leaseStore
            .Received(1)
            .ReleaseAsync(
                execution.JobKey,
                execution.ExecutionId,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT105_Execute_Should_Release_Lease_When_Job_Task_Faults()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var heartbeatService =
            CreateHeartbeatService();

        heartbeatService
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);

        var execution =
            TestData.CreateExecution(
                "owner-a");

        var options =
            TestData.CreateOptions();

        // Act

        var result =
            await coordinator.ExecuteAsync(
                execution,
                options,
                _ =>
                    Task.FromException(
                        new InvalidOperationException(
                            "Async failure")));

        // Assert

        Assert.Equal(
            ExecutionOutcome.Failed,
            result.Outcome);

        Assert.IsType<InvalidOperationException>(
            result.Exception);

        await leaseStore
            .Received(1)
            .ReleaseAsync(
                execution.JobKey,
                execution.ExecutionId,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT140_Execute_Should_Start_Heartbeat()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var heartbeatService =
            CreateHeartbeatService();

        heartbeatService
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);

        var execution =
            TestData.CreateExecution(
                "owner-a");

        var options =
            TestData.CreateOptions();

        // Act

        await coordinator.ExecuteAsync(
            execution,
            options,
            _ => Task.CompletedTask);

        // Assert

        await heartbeatService
            .Received(1)
            .RunAsync(
                execution,
                options,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT141_Execute_Should_Stop_Heartbeat_When_Job_Completes()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var heartbeatService =
            CreateHeartbeatService();

        var heartbeatCancellationObserved =
            false;

        heartbeatService
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(
                async call =>
                {
                    var token =
                        call.ArgAt<CancellationToken>(2);

                    try
                    {
                        await Task.Delay(
                            Timeout.InfiniteTimeSpan,
                            token);
                    }
                    catch (OperationCanceledException)
                    {
                        heartbeatCancellationObserved =
                            true;
                    }

                    return true;
                });

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);

        var execution =
            TestData.CreateExecution(
                "owner-a");

        var options =
            TestData.CreateOptions();

        // Act

        await coordinator.ExecuteAsync(
            execution,
            options,
            _ => Task.CompletedTask);

        // Assert

        Assert.True(
            heartbeatCancellationObserved);

        await heartbeatService
            .Received(1)
            .RunAsync(
                execution,
                options,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT150_Execute_Should_Cancel_Job_When_Lease_Is_Lost()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var heartbeatService =
            CreateHeartbeatService();

        heartbeatService
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(50);

                return false;
            });

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);

        var execution =
            TestData.CreateExecution(
                "owner-a");

        var options =
            TestData.CreateOptions();

        var tokenCancelled =
            false;

        // Act

        var executeTask =
            coordinator.ExecuteAsync(
                execution,
                options,
                async ct =>
                {
                    try
                    {
                        await Task.Delay(
                            Timeout.InfiniteTimeSpan,
                            ct);
                    }
                    catch (OperationCanceledException)
                    {
                        tokenCancelled = true;
                    }
                });

        var completedTask =
            await Task.WhenAny(
                executeTask,
                Task.Delay(
                    TimeSpan.FromSeconds(5)));

        Assert.Same(
            executeTask,
            completedTask);

        var result =
            await executeTask;

        // Assert

        Assert.True(
            tokenCancelled);

        Assert.Equal(
            ExecutionOutcome.LeaseLost,
            result.Outcome);
    }

    [Fact]
    public async Task CT151_Execute_Should_Release_Lease_When_Heartbeat_Fails()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var heartbeatService =
            CreateHeartbeatService();

        heartbeatService
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(50);

                return false;
            });

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);

        var execution =
            TestData.CreateExecution(
                "owner-a");

        var options =
            TestData.CreateOptions();

        // Act

        var result =
            await coordinator.ExecuteAsync(
                execution,
                options,
                async ct =>
                {
                    try
                    {
                        await Task.Delay(
                            Timeout.InfiniteTimeSpan,
                            ct);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });

        // Assert

        Assert.Equal(
            ExecutionOutcome.LeaseLost,
            result.Outcome);

        await leaseStore
            .Received(1)
            .ReleaseAsync(
                execution.JobKey,
                execution.ExecutionId,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT154_Execute_Should_Stop_When_External_Cancellation_Is_Requested()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var heartbeatService =
            CreateHeartbeatService();

        heartbeatService
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var token =
                    call.ArgAt<CancellationToken>(2);

                try
                {
                    await Task.Delay(
                        Timeout.InfiniteTimeSpan,
                        token);
                }
                catch (OperationCanceledException)
                {
                }

                return true;
            });

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);

        var execution =
            TestData.CreateExecution(
                "owner-a");

        var options =
            TestData.CreateOptions();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var jobCancelled =
            false;

        // Act

        var executeTask =
            coordinator.ExecuteAsync(
                execution,
                options,
                async ct =>
                {
                    try
                    {
                        await Task.Delay(
                            Timeout.InfiniteTimeSpan,
                            ct);
                    }
                    catch (OperationCanceledException)
                    {
                        jobCancelled = true;

                        throw;
                    }
                },
                cancellationTokenSource.Token);

        cancellationTokenSource.CancelAfter(
            TimeSpan.FromMilliseconds(50));

        // Assert

        var result =
            await executeTask;

        Assert.True(
            jobCancelled);

        Assert.Equal(
            ExecutionOutcome.Cancelled,
            result.Outcome);

        await leaseStore
            .Received(1)
            .ReleaseAsync(
                execution.JobKey,
                execution.ExecutionId,
                Arg.Any<CancellationToken>());
    }
}