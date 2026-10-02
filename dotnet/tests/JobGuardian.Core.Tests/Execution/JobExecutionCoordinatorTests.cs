using JobGuardian.Abstractions.Models;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Core.Execution;
using JobGuardian.Core.Models;
using JobGuardian.Core.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
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

    [Fact]
    public async Task CT160_Execute_When_Callback_Fails_Then_Succeeds_Should_Retry_Under_One_Lease()
    {
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
                    when (token.IsCancellationRequested)
                {
                }

                return true;
            });

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);

        var calls = 0;
        var options =
            TestData.CreateOptions() with
            {
                MaxAttempts = 3,
                RetryDelay = TimeSpan.Zero
            };

        var result =
            await coordinator.ExecuteAsync(
                TestData.CreateExecution("owner-a"),
                options,
                _ =>
                {
                    if (Interlocked.Increment(ref calls) == 1)
                    {
                        throw new InvalidOperationException(
                            "Transient callback failure.");
                    }

                    return Task.CompletedTask;
                });

        Assert.Equal(
            ExecutionOutcome.Succeeded,
            result.Outcome);
        Assert.Equal(
            2,
            calls);

        await leaseStore
            .Received(1)
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>());
        await leaseStore
            .Received(1)
            .ReleaseAsync(
                Arg.Any<JobKey>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
        await heartbeatService
            .Received(1)
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT161_Execute_When_All_Callback_Attempts_Fail_Should_Return_Final_Failure()
    {
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
                    when (token.IsCancellationRequested)
                {
                }

                return true;
            });

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);
        var calls = 0;

        var result =
            await coordinator.ExecuteAsync(
                TestData.CreateExecution("owner-a"),
                TestData.CreateOptions() with
                {
                    MaxAttempts = 3,
                    RetryDelay = TimeSpan.Zero
                },
                _ =>
                {
                    Interlocked.Increment(ref calls);
                    throw new InvalidOperationException(
                        "Callback failure.");
                });

        Assert.Equal(
            ExecutionOutcome.Failed,
            result.Outcome);
        Assert.IsType<InvalidOperationException>(
            result.Exception);
        Assert.Equal(
            3,
            calls);
    }

    [Fact]
    public async Task CT162_Execute_When_Cancelled_During_Retry_Delay_Should_Not_Start_Next_Attempt()
    {
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
                    when (token.IsCancellationRequested)
                {
                }

                return true;
            });

        using var cancellationTokenSource =
            new CancellationTokenSource();
        var retryScheduled =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService,
                new RetryScheduledLogger(
                    () => retryScheduled.TrySetResult()));
        var calls = 0;

        var executeTask =
            coordinator.ExecuteAsync(
                TestData.CreateExecution("owner-a"),
                TestData.CreateOptions() with
                {
                    MaxAttempts = 3,
                    RetryDelay = TimeSpan.FromMinutes(1)
                },
                _ =>
                {
                    Interlocked.Increment(ref calls);
                    throw new InvalidOperationException(
                        "Transient callback failure.");
                },
                cancellationTokenSource.Token);

        await retryScheduled.Task;
        cancellationTokenSource.Cancel();

        var result =
            await executeTask;

        Assert.Equal(
            ExecutionOutcome.Cancelled,
            result.Outcome);
        Assert.Equal(
            1,
            calls);
    }

    [Fact]
    public async Task CT163_Execute_When_Lease_Is_Lost_During_Retry_Delay_Should_Not_Start_Next_Attempt()
    {
        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var heartbeatCompletion =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        var heartbeatService =
            CreateHeartbeatService();

        heartbeatService
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => heartbeatCompletion.Task);

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService);
        var callbackInvoked =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;

        var executeTask =
            coordinator.ExecuteAsync(
                TestData.CreateExecution("owner-a"),
                TestData.CreateOptions() with
                {
                    MaxAttempts = 3,
                    RetryDelay = TimeSpan.FromMinutes(1)
                },
                _ =>
                {
                    Interlocked.Increment(ref calls);
                    callbackInvoked.TrySetResult();
                    throw new InvalidOperationException(
                        "Transient callback failure.");
                });

        await callbackInvoked.Task;
        heartbeatCompletion.SetResult(false);

        var result =
            await executeTask;

        Assert.Equal(
            ExecutionOutcome.LeaseLost,
            result.Outcome);
        Assert.Equal(
            1,
            calls);
    }

    [Fact]
    public async Task CT164_Execute_When_Lease_Is_Lost_Before_Immediate_Retry_Should_Not_Start_Next_Attempt()
    {
        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var heartbeatCompletion =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        var heartbeatService =
            CreateHeartbeatService();

        heartbeatService
            .RunAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<JobExecutionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => heartbeatCompletion.Task);

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                heartbeatService,
                new RetryScheduledLogger(
                    () => heartbeatCompletion.TrySetResult(false)));
        var calls = 0;

        var result =
            await coordinator.ExecuteAsync(
                TestData.CreateExecution("owner-a"),
                TestData.CreateOptions() with
                {
                    MaxAttempts = 3,
                    RetryDelay = TimeSpan.Zero
                },
                _ =>
                {
                    Interlocked.Increment(ref calls);
                    throw new InvalidOperationException(
                        "Transient callback failure.");
                });

        Assert.Equal(
            ExecutionOutcome.LeaseLost,
            result.Outcome);
        Assert.Equal(
            1,
            calls);
    }

    [Fact]
    public async Task CT165_Execute_When_Retry_Options_Are_Invalid_Should_Throw_Before_Acquiring_Lease()
    {
        var leaseStore =
            CreateLeaseStore();
        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                CreateHeartbeatService());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => coordinator.ExecuteAsync(
                TestData.CreateExecution("owner-a"),
                TestData.CreateOptions() with
                {
                    MaxAttempts = 11
                },
                _ => Task.CompletedTask));

        await leaseStore
            .DidNotReceive()
            .TryAcquireAsync(
                Arg.Any<ActiveExecution>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>());
    }

    private sealed class RetryScheduledLogger
        : ILogger<JobExecutionCoordinator>
    {
        private readonly Action _onWarning;

        public RetryScheduledLogger(
            Action onWarning)
        {
            _onWarning = onWarning;
        }

        public IDisposable? BeginScope<TState>(
            TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(
            LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                _onWarning();
            }
        }
    }
}