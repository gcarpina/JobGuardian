using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

using JobGuardian.PostgreSql.IntegrationTests.Infrastructure;

namespace JobGuardian.PostgreSql.IntegrationTests;

[Collection("postgresql")]
public sealed class JobStateRepositoryTests
    : PostgreSqlTestBase
{
    public JobStateRepositoryTests(
        PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task CT1000_Get_Missing_State_Should_Return_Null()
    {
        await Fixture.ResetDatabaseAsync();

        // Arrange

        var repository =
            CreateJobStateRepository();

        var jobKey =
            new JobKey(
                TenantId,
                JobNamespace,
                JobName);

        // Act

        var state =
            await repository.GetAsync(
                jobKey);

        // Assert

        Assert.Null(
            state);
    }

    [Fact]
    public async Task CT1010_Set_Blocked_State_Should_Persist()
    {
        await Fixture.ResetDatabaseAsync();

        // Arrange

        var repository =
            CreateJobStateRepository();

        var jobKey =
            new JobKey(
                TenantId,
                JobNamespace,
                JobName);

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

    [Fact]
    public async Task CT1020_Set_Eligible_Should_Remove_Persisted_State()
    {
        await Fixture.ResetDatabaseAsync();

        // Arrange

        var repository =
            CreateJobStateRepository();

        var jobKey =
            new JobKey(
                TenantId,
                JobNamespace,
                JobName);

        await repository.SetAsync(
            jobKey,
            JobState.Blocked);

        // Act

        await repository.SetAsync(
            jobKey,
            JobState.Eligible);

        var state =
            await repository.GetAsync(
                jobKey);

        // Assert

        Assert.Null(
            state);
    }

    [Fact]
    public async Task CT1030_Different_JobKeys_Should_Remain_Isolated()
    {
        await Fixture.ResetDatabaseAsync();

        // Arrange

        var repository =
            CreateJobStateRepository();

        var jobA =
            new JobKey(
                "tenant-a",
                JobNamespace,
                JobName);

        var jobB =
            new JobKey(
                "tenant-b",
                JobNamespace,
                JobName);

        // Act

        await repository.SetAsync(
            jobA,
            JobState.Blocked);

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

        Assert.Null(
            stateB);
    }

    [Fact]
    public async Task CT1040_Set_Blocked_Twice_Should_Be_Idempotent()
    {
        await Fixture.ResetDatabaseAsync();

        // Arrange

        var repository =
            CreateJobStateRepository();

        var jobKey =
            new JobKey(
                TenantId,
                JobNamespace,
                JobName);

        // Act

        await repository.SetAsync(
            jobKey,
            JobState.Blocked);

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