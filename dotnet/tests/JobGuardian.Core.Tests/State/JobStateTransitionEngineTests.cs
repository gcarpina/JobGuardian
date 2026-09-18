using JobGuardian.Abstractions.Enums;
using JobGuardian.Core.State;

namespace JobGuardian.Core.Tests.State;

public sealed class JobStateTransitionEngineTests
{
    [Fact]
    public void CT600_Running_And_Continue_Should_Return_Eligible()
    {
        // Arrange

        var engine =
            new JobStateTransitionEngine();

        // Act

        var state =
            engine.Apply(
                JobState.Running,
                PolicyDecision.Continue);

        // Assert

        Assert.Equal(
            JobState.Eligible,
            state);
    }

    [Fact]
    public void CT610_Running_And_Block_Should_Return_Blocked()
    {
        // Arrange

        var engine =
            new JobStateTransitionEngine();

        // Act

        var state =
            engine.Apply(
                JobState.Running,
                PolicyDecision.Block);

        // Assert

        Assert.Equal(
            JobState.Blocked,
            state);
    }

    [Fact]
    public void CT620_Eligible_And_Continue_Should_Throw()
    {
        // Arrange

        var engine =
            new JobStateTransitionEngine();

        // Act + Assert

        Assert.Throws<InvalidOperationException>(
            () =>
                engine.Apply(
                    JobState.Eligible,
                    PolicyDecision.Continue));
    }

    [Fact]
    public void CT630_Blocked_And_Continue_Should_Throw()
    {
        // Arrange

        var engine =
            new JobStateTransitionEngine();

        // Act + Assert

        Assert.Throws<InvalidOperationException>(
            () =>
                engine.Apply(
                    JobState.Blocked,
                    PolicyDecision.Continue));
    }

    [Fact]
    public void CT640_Blocked_And_Block_Should_Throw()
    {
        // Arrange

        var engine =
            new JobStateTransitionEngine();

        // Act + Assert

        Assert.Throws<InvalidOperationException>(
            () =>
                engine.Apply(
                    JobState.Blocked,
                    PolicyDecision.Block));
    }
}
