using JobGuardian.Abstractions.Enums;
using JobGuardian.Core.Execution;

namespace JobGuardian.Core.Tests.Execution;

public sealed class ExecutionOutcomeMapperTests
{
    [Fact]
    public void CT400_Should_Map_Success_To_Succeeded()
    {
        var outcome =
            ExecutionOutcomeMapper.FromSuccess();

        Assert.Equal(
            ExecutionOutcome.Succeeded,
            outcome);
    }

    [Fact]
    public void CT410_Should_Map_Failure_To_Failed()
    {
        var outcome =
            ExecutionOutcomeMapper.FromFailure();

        Assert.Equal(
            ExecutionOutcome.Failed,
            outcome);
    }

    [Fact]
    public void CT420_Should_Map_Lease_Not_Acquired_To_Skipped()
    {
        var outcome =
            ExecutionOutcomeMapper.FromLeaseNotAcquired();

        Assert.Equal(
            ExecutionOutcome.Skipped,
            outcome);
    }

    [Fact]
    public void CT430_Should_Map_Lease_Lost_To_LeaseLost()
    {
        var outcome =
            ExecutionOutcomeMapper.FromLeaseLost();

        Assert.Equal(
            ExecutionOutcome.LeaseLost,
            outcome);
    }

    [Fact]
    public void CT440_Should_Map_Cancellation_To_Cancelled()
    {
        var outcome =
            ExecutionOutcomeMapper.FromCancellation();

        Assert.Equal(
            ExecutionOutcome.Cancelled,
            outcome);
    }
}