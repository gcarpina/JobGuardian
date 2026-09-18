using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Policies;
using JobGuardian.Core.State;

namespace JobGuardian.Core.Tests.State;

public sealed class JobStateManagerResetTests
{
    [Fact]
    public async Task CT1100_Reset_Should_Remove_Blocked_State()
    {
        // Arrange

        var repository =
            new InMemoryJobStateRepository();

        var manager =
            new JobStateManager(
                new FailurePolicyEvaluator(),
                new JobStateTransitionEngine(),
                repository);

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        await repository.SetAsync(
            jobKey,
            JobState.Blocked);

        // Act

        await manager.ResetAsync(
            jobKey);

        var state =
            await manager.GetCurrentStateAsync(
                jobKey);

        // Assert

        Assert.Equal(
            JobState.Eligible,
            state);
    }

    [Fact]
    public async Task CT1110_Reset_Should_Be_Idempotent()
    {
        // Arrange

        var repository =
            new InMemoryJobStateRepository();

        var manager =
            new JobStateManager(
                new FailurePolicyEvaluator(),
                new JobStateTransitionEngine(),
                repository);

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        // Act

        await manager.ResetAsync(
            jobKey);

        await manager.ResetAsync(
            jobKey);

        var state =
            await manager.GetCurrentStateAsync(
                jobKey);

        // Assert

        Assert.Equal(
            JobState.Eligible,
            state);
    }

    [Fact]
    public async Task CT1120_Reset_Should_Remove_Persisted_Record()
    {
        // Arrange

        var repository =
            new InMemoryJobStateRepository();

        var manager =
            new JobStateManager(
                new FailurePolicyEvaluator(),
                new JobStateTransitionEngine(),
                repository);

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        await repository.SetAsync(
            jobKey,
            JobState.Blocked);

        // Act

        await manager.ResetAsync(
            jobKey);

        var persistedState =
            await repository.GetAsync(
                jobKey);

        // Assert

        Assert.Equal(
            JobState.Eligible,
            persistedState);
    }
}