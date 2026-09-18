using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Core.Tests.State;

public sealed class JobStateTests
{
    [Fact]
    public void CT580_Should_Define_Eligible_State()
    {
        Assert.Equal(
            0,
            (int)JobState.Eligible);
    }

    [Fact]
    public void CT581_Should_Define_Running_State()
    {
        Assert.Equal(
            1,
            (int)JobState.Running);
    }

    [Fact]
    public void CT582_Should_Define_Blocked_State()
    {
        Assert.Equal(
            2,
            (int)JobState.Blocked);
    }
}