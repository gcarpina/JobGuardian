using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Abstractions.Contracts;
using JobGuardian.Core.Audit;
using JobGuardian.Core.Policies;
using JobGuardian.Core.State;
using NSubstitute;

namespace JobGuardian.Core.Tests.State;

public sealed class JobStateManagerResetTests
{
    [Fact]
    public async Task CT1100_Reset_Should_Remove_Blocked_State()
    {
        // Arrange

        var repository =
            new InMemoryJobStateRepository();
        var auditStore =
            new InMemoryAuditStore();

        var manager =
            new JobStateManager(
                new FailurePolicyEvaluator(),
                new JobStateTransitionEngine(),
                repository,
                auditStore);

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
            jobKey,
            "operator-42");

        var state =
            await manager.GetCurrentStateAsync(
                jobKey);

        // Assert

        Assert.Equal(
            JobState.Eligible,
            state);
        var auditEntry =
            Assert.Single(auditStore.Entries);
        Assert.Equal("operator-42", auditEntry.UserId);
        Assert.Equal("ResetJobRequested", auditEntry.Action);
        Assert.Equal(jobKey.TenantId, auditEntry.TenantId);
        Assert.Equal(jobKey.JobNamespace, auditEntry.JobNamespace);
        Assert.Equal(jobKey.JobName, auditEntry.JobName);
        Assert.Null(auditEntry.UserName);
        Assert.Equal(TimeSpan.Zero, auditEntry.TimestampUtc.Offset);
    }

    [Fact]
    public async Task CT1110_Reset_Should_Be_Idempotent()
    {
        // Arrange

        var repository =
            new InMemoryJobStateRepository();
        var auditStore =
            new InMemoryAuditStore();

        var manager =
            new JobStateManager(
                new FailurePolicyEvaluator(),
                new JobStateTransitionEngine(),
                repository,
                auditStore);

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        // Act

        await manager.ResetAsync(
            jobKey,
            "operator-42");

        await manager.ResetAsync(
            jobKey,
            "operator-42");

        var state =
            await manager.GetCurrentStateAsync(
                jobKey);

        // Assert

        Assert.Equal(
            JobState.Eligible,
            state);
        Assert.Equal(2, auditStore.Entries.Count);
    }

    [Fact]
    public async Task CT1120_Reset_Should_Remove_Persisted_Record()
    {
        // Arrange

        var repository =
            new InMemoryJobStateRepository();
        var auditStore =
            new InMemoryAuditStore();

        var manager =
            new JobStateManager(
                new FailurePolicyEvaluator(),
                new JobStateTransitionEngine(),
                repository,
                auditStore);

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
            jobKey,
            "operator-42");

        var persistedState =
            await repository.GetAsync(
                jobKey);

        // Assert

        Assert.Equal(
            JobState.Eligible,
            persistedState);
    }

    [Fact]
    public async Task CT1130_Reset_Should_Not_Change_State_When_Audit_Write_Fails()
    {
        var repository =
            Substitute.For<IJobStateRepository>();
        var auditStore =
            Substitute.For<IAuditStore>();
        auditStore
            .WriteAsync(
                Arg.Any<AuditEntry>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(
                _ => throw new InvalidOperationException("Audit store unavailable."));

        var manager =
            new JobStateManager(
                new FailurePolicyEvaluator(),
                new JobStateTransitionEngine(),
                repository,
                auditStore);
        var jobKey =
            new JobKey("tenant-a", "billing", "invoice-sync");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.ResetAsync(jobKey, "operator-42"));

        await repository
            .DidNotReceive()
            .SetAsync(
                Arg.Any<JobKey>(),
                Arg.Any<JobState>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT1140_Reset_Should_Reject_Empty_Actor_Without_Writing_Audit()
    {
        var repository =
            Substitute.For<IJobStateRepository>();
        var auditStore =
            Substitute.For<IAuditStore>();
        var manager =
            new JobStateManager(
                new FailurePolicyEvaluator(),
                new JobStateTransitionEngine(),
                repository,
                auditStore);

        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.ResetAsync(
                new JobKey("tenant-a", "billing", "invoice-sync"),
                " "));

        await auditStore
            .DidNotReceive()
            .WriteAsync(
                Arg.Any<AuditEntry>(),
                Arg.Any<CancellationToken>());
    }
}