using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Policies;

namespace JobGuardian.Core.Tests.Policies;

public sealed class FailurePolicyEvaluatorTests
{
    [Fact]
    public void CT500_Failed_And_Ignore_Should_Return_Continue()
    {
        // Arrange

        var evaluator =
            new FailurePolicyEvaluator();

        var result =
            new ExecutionResult(
                ExecutionOutcome.Failed);

        // Act

        var decision =
            evaluator.Evaluate(
                result,
                FailurePolicy.Ignore);

        // Assert

        Assert.Equal(
            PolicyDecision.Continue,
            decision);
    }

    [Fact]
    public void CT510_Failed_And_RequireManualReset_Should_Return_Block()
    {
        // Arrange

        var evaluator =
            new FailurePolicyEvaluator();

        var result =
            new ExecutionResult(
                ExecutionOutcome.Failed);

        // Act

        var decision =
            evaluator.Evaluate(
                result,
                FailurePolicy.RequireManualReset);

        // Assert

        Assert.Equal(
            PolicyDecision.Block,
            decision);
    }

    [Fact]
    public void CT520_LeaseLost_And_Ignore_Should_Return_Continue()
    {
        // Arrange

        var evaluator =
            new FailurePolicyEvaluator();

        var result =
            new ExecutionResult(
                ExecutionOutcome.LeaseLost);

        // Act

        var decision =
            evaluator.Evaluate(
                result,
                FailurePolicy.Ignore);

        // Assert

        Assert.Equal(
            PolicyDecision.Continue,
            decision);
    }

    [Fact]
    public void CT530_LeaseLost_And_RequireManualReset_Should_Return_Block()
    {
        // Arrange

        var evaluator =
            new FailurePolicyEvaluator();

        var result =
            new ExecutionResult(
                ExecutionOutcome.LeaseLost);

        // Act

        var decision =
            evaluator.Evaluate(
                result,
                FailurePolicy.RequireManualReset);

        // Assert

        Assert.Equal(
            PolicyDecision.Block,
            decision);
    }

    [Fact]
    public void CT540_Succeeded_And_RequireManualReset_Should_Return_Continue()
    {
        // Arrange

        var evaluator =
            new FailurePolicyEvaluator();

        var result =
            new ExecutionResult(
                ExecutionOutcome.Succeeded);

        // Act

        var decision =
            evaluator.Evaluate(
                result,
                FailurePolicy.RequireManualReset);

        // Assert

        Assert.Equal(
            PolicyDecision.Continue,
            decision);
    }

    [Fact]
    public void CT550_Skipped_And_RequireManualReset_Should_Return_Continue()
    {
        // Arrange

        var evaluator =
            new FailurePolicyEvaluator();

        var result =
            new ExecutionResult(
                ExecutionOutcome.Skipped);

        // Act

        var decision =
            evaluator.Evaluate(
                result,
                FailurePolicy.RequireManualReset);

        // Assert

        Assert.Equal(
            PolicyDecision.Continue,
            decision);
    }
}