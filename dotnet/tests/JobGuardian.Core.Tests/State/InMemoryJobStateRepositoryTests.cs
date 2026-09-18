using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.State;

namespace JobGuardian.Core.Tests.State;

public sealed class InMemoryJobStateRepositoryTests
{
    [Fact]
    public async Task CT700_Get_Unknown_State_Should_Return_Null()
    {
        // Arrange

        var repository =
            new InMemoryJobStateRepository();

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        // Act

        var state =
            await repository.GetAsync(
                jobKey);

        // Assert

        Assert.Null(
            state);
    }

    [Fact]
    public async Task CT710_Set_Then_Get_Should_Return_State()
    {
        // Arrange

        var repository =
            new InMemoryJobStateRepository();

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        // Act

        await repository.SetAsync(
            jobKey,
            JobState.Eligible);

        var state =
            await repository.GetAsync(
                jobKey);

        // Assert

        Assert.Equal(
            JobState.Eligible,
            state);
    }

    [Fact]
    public async Task CT720_Set_Should_Overwrite_Previous_State()
    {
        // Arrange

        var repository =
            new InMemoryJobStateRepository();

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        // Act

        await repository.SetAsync(
            jobKey,
            JobState.Eligible);

        await repository.SetAsync(
            jobKey,
            JobState.Blocked);

        var state =
            await repository.GetAsync(
                jobKey);

        // Assert

        Assert.Equal(
            JobState.Blocked,
            state);
    }

    [Fact]
    public async Task CT730_Different_Jobs_Should_Have_Independent_State()
    {
        // Arrange

        var repository =
            new InMemoryJobStateRepository();

        var jobA =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        var jobB =
            new JobKey(
                "tenant-b",
                "billing",
                "invoice-sync");

        // Act

        await repository.SetAsync(
            jobA,
            JobState.Blocked);

        await repository.SetAsync(
            jobB,
            JobState.Eligible);

        var stateA =
            await repository.GetAsync(
                jobA);

        var stateB =
            await repository.GetAsync(
                jobB);

        // Assert

        Assert.Equal(
            JobState.Blocked,
            stateA);

        Assert.Equal(
            JobState.Eligible,
            stateB);
    }

    [Fact]
    public async Task CT740_Blocked_State_Should_Be_Retrievable()
    {
        // Arrange

        var repository =
            new InMemoryJobStateRepository();

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        // Act

        await repository.SetAsync(
            jobKey,
            JobState.Blocked);

        var state =
            await repository.GetAsync(
                jobKey);

        // Assert

        Assert.Equal(
            JobState.Blocked,
            state);
    }
}