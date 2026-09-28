using JobGuardian.Abstractions.Models;
using JobGuardian.Core.LeaseStore;

namespace JobGuardian.Core.Tests.LeaseStore;

public sealed class InMemoryLeaseStoreTests
{
    private static readonly JobKey JobKey =
        new(
            "tenant-a",
            "billing",
            "invoice-sync");

    [Fact]
    public async Task TryAcquireAsync_Should_Allow_Only_One_Concurrent_Owner()
    {
        var store =
            new InMemoryLeaseStore();

        var executions =
            Enumerable.Range(0, 20)
                .Select(
                    _ => new ActiveExecution
                    {
                        JobKey = JobKey,
                        ExecutionId = Guid.NewGuid(),
                        OwnerId = Guid.NewGuid().ToString()
                    })
                .ToArray();

        var acquireTasks =
            executions.Select(
                execution => Task.Run(
                    () => store.TryAcquireAsync(
                        execution,
                        TimeSpan.FromMinutes(1))));

        var results =
            await Task.WhenAll(
                acquireTasks);

        Assert.Single(
            results,
            acquired => acquired);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_Transfer_Expired_Lease()
    {
        var store =
            new InMemoryLeaseStore();

        var firstExecution =
            CreateExecution();

        var secondExecution =
            CreateExecution();

        Assert.True(
            await store.TryAcquireAsync(
                firstExecution,
                TimeSpan.FromMilliseconds(1)));

        await Task.Delay(
            TimeSpan.FromMilliseconds(25));

        Assert.True(
            await store.TryAcquireAsync(
                secondExecution,
                TimeSpan.FromMinutes(1)));

        var activeLease =
            await store.GetActiveLeaseAsync(
                JobKey);

        Assert.Equal(
            secondExecution.ExecutionId,
            activeLease?.ExecutionId);
    }

    [Fact]
    public async Task RenewAsync_Should_Require_Current_Owner_And_Active_Lease()
    {
        var store =
            new InMemoryLeaseStore();

        var execution =
            CreateExecution();

        Assert.True(
            await store.TryAcquireAsync(
                execution,
                TimeSpan.FromMinutes(1)));

        Assert.False(
            await store.RenewAsync(
                JobKey,
                Guid.NewGuid(),
                TimeSpan.FromMinutes(1)));

        Assert.True(
            await store.RenewAsync(
                JobKey,
                execution.ExecutionId,
                TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public async Task ReleaseAsync_Should_Not_Release_Another_Execution_Lease()
    {
        var store =
            new InMemoryLeaseStore();

        var execution =
            CreateExecution();

        Assert.True(
            await store.TryAcquireAsync(
                execution,
                TimeSpan.FromMinutes(1)));

        Assert.False(
            await store.ReleaseAsync(
                JobKey,
                Guid.NewGuid()));

        Assert.NotNull(
            await store.GetActiveLeaseAsync(
                JobKey));

        Assert.True(
            await store.ReleaseAsync(
                JobKey,
                execution.ExecutionId));

        Assert.Null(
            await store.GetActiveLeaseAsync(
                JobKey));
    }

    private static ActiveExecution CreateExecution()
    {
        return new ActiveExecution
        {
            JobKey = JobKey,
            ExecutionId = Guid.NewGuid(),
            OwnerId = "test-instance"
        };
    }
}
