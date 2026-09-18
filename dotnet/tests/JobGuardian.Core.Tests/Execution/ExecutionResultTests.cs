
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Tests.Execution;

public sealed class ExecutionResultTests
{
    [Fact]
    public void CT450_Should_Create_Succeeded_Result()
    {
        var result =
            new ExecutionResult(
                ExecutionOutcome.Succeeded);

        Assert.Equal(
            ExecutionOutcome.Succeeded,
            result.Outcome);

        Assert.Null(
            result.Exception);
    }

    [Fact]
    public void CT460_Should_Create_Failed_Result_With_Exception()
    {
        var exception =
            new InvalidOperationException(
                "Boom");

        var result =
            new ExecutionResult(
                ExecutionOutcome.Failed,
                exception);

        Assert.Equal(
            ExecutionOutcome.Failed,
            result.Outcome);

        Assert.Same(
            exception,
            result.Exception);
    }
}