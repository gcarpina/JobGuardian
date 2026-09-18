using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Execution;
using JobGuardian.Core.Models;
using JobGuardian.Core.Tests.Infrastructure;

using NSubstitute;

namespace JobGuardian.Core.Tests.Execution;

public sealed class LeaseHeartbeatServiceTests
    : CoreTestBase
{
    [Fact]
    public async Task CT112_RunAsync_Should_Stop_When_Lease_Is_Lost()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        var execution =
            TestData.CreateExecution(
                "owner-a");

        leaseStore
            .RenewAsync(
                Arg.Any<JobKey>(),
                Arg.Any<Guid>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var heartbeat =
            new LeaseHeartbeatService(
                leaseStore);

        var options =
            TestData.CreateOptions();

        // Act

        var result =
            await heartbeat.RunAsync(
                execution,
                options);

        // Assert

        Assert.False(
            result);
    }

    [Fact]
    public async Task CT130_RunAsync_Should_Renew_Until_Cancelled()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        var execution =
            TestData.CreateExecution(
                "owner-a");

        leaseStore
            .RenewAsync(
                Arg.Any<JobKey>(),
                Arg.Any<Guid>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var heartbeat =
            new LeaseHeartbeatService(
                leaseStore);

        var options =
            TestData.CreateOptions();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        // Act

        var task =
            heartbeat.RunAsync(
                execution,
                options,
                cancellationTokenSource.Token);

        await Task.Delay(
            50);

        cancellationTokenSource.Cancel();

        await task;

        // Assert

        var renewCalls =
            leaseStore
                .ReceivedCalls()
                .Count(call =>
                    call.GetMethodInfo().Name ==
                    nameof(ILeaseStore.RenewAsync));

        Assert.True(
            renewCalls >= 2);
    }

    [Fact]
    public async Task CT131_RunAsync_Should_Stop_After_Renew_Failure()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        var execution =
            TestData.CreateExecution(
                "owner-a");

        leaseStore
            .RenewAsync(
                Arg.Any<JobKey>(),
                Arg.Any<Guid>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(
                true,
                true,
                false);

        var heartbeat =
            new LeaseHeartbeatService(
                leaseStore);

        var options =
            TestData.CreateOptions();

        // Act

        var result =
            await heartbeat.RunAsync(
                execution,
                options);

        // Assert

        Assert.False(
            result);

        await leaseStore
            .Received(3)
            .RenewAsync(
                execution.JobKey,
                execution.ExecutionId,
                options.LeaseDuration,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT132_RunAsync_Should_Propagate_LeaseStore_Exception()
    {
        // Arrange

        var leaseStore =
            CreateLeaseStore();

        var execution =
            TestData.CreateExecution(
                "owner-a");

        leaseStore
            .RenewAsync(
                Arg.Any<JobKey>(),
                Arg.Any<Guid>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(
                _ => throw new InvalidOperationException(
                    "Renew failed"));

        var heartbeat =
            new LeaseHeartbeatService(
                leaseStore);

        var options =
            TestData.CreateOptions();

        // Act + Assert

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                heartbeat.RunAsync(
                    execution,
                    options));
    }
}