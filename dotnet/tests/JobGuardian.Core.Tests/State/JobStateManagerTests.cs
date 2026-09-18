using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Policies;
using JobGuardian.Core.State;
using NSubstitute;
using JobGuardian.Abstractions.Contracts;

namespace JobGuardian.Core.Tests.State;

public sealed class JobStateManagerTests
{
    [Fact]
    public async Task CT800_GetCurrentState_Should_Return_Eligible_When_State_Does_Not_Exist()
    {
        // Arrange

        var repository =
            Substitute.For<IJobStateRepository>();

        repository
            .GetAsync(
                Arg.Any<JobKey>(),
                Arg.Any<CancellationToken>())
            .Returns((JobState?)null);

        var manager =
            CreateManager(
                repository);

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        // Act

        var state =
            await manager.GetCurrentStateAsync(
                jobKey);

        // Assert

        Assert.Equal(
            JobState.Eligible,
            state);
    }

    [Fact]
    public async Task CT810_Should_Block_Job_After_Failed_Execution_With_RequireManualReset()
    {
        // Arrange

        var repository =
            Substitute.For<IJobStateRepository>();

        repository
            .GetAsync(
                Arg.Any<JobKey>(),
                Arg.Any<CancellationToken>())
            .Returns((JobState?)null);

        var evaluator =
            Substitute.For<IFailurePolicyEvaluator>();

        evaluator
            .Evaluate(
                Arg.Any<ExecutionResult>(),
                FailurePolicy.RequireManualReset)
            .Returns(
                PolicyDecision.Block);

        var transitionEngine =
            Substitute.For<IJobStateTransitionEngine>();

        transitionEngine
            .Apply(
                JobState.Running,
                PolicyDecision.Block)
            .Returns(
                JobState.Blocked);

        var manager =
            new JobStateManager(
                evaluator,
                transitionEngine,
                repository);

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        var result =
            new ExecutionResult(
                ExecutionOutcome.Failed);

        // Act

        await manager.HandleExecutionResultAsync(
            jobKey,
            FailurePolicy.RequireManualReset,
            result);

        // Assert

        await repository
            .Received(1)
            .SetAsync(
                jobKey,
                JobState.Blocked,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT820_Should_Keep_Job_Eligible_After_Failed_Execution_With_Ignore()
    {
        // Arrange

        var repository =
            Substitute.For<IJobStateRepository>();

        repository
            .GetAsync(
                Arg.Any<JobKey>(),
                Arg.Any<CancellationToken>())
            .Returns((JobState?)null);

        var evaluator =
            Substitute.For<IFailurePolicyEvaluator>();

        evaluator
            .Evaluate(
                Arg.Any<ExecutionResult>(),
                FailurePolicy.Ignore)
            .Returns(
                PolicyDecision.Continue);

        var transitionEngine =
            Substitute.For<IJobStateTransitionEngine>();

        transitionEngine
            .Apply(
                JobState.Running,
                PolicyDecision.Continue)
            .Returns(
                JobState.Eligible);

        var manager =
            new JobStateManager(
                evaluator,
                transitionEngine,
                repository);

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        var result =
            new ExecutionResult(
                ExecutionOutcome.Failed);

        // Act

        await manager.HandleExecutionResultAsync(
            jobKey,
            FailurePolicy.Ignore,
            result);

        // Assert

        await repository
            .Received(1)
            .SetAsync(
                jobKey,
                JobState.Eligible,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT830_Should_Persist_Blocked_State()
    {
        // Arrange

        var repository =
            Substitute.For<IJobStateRepository>();

        repository
            .GetAsync(
                Arg.Any<JobKey>(),
                Arg.Any<CancellationToken>())
            .Returns((JobState?)null);

        var evaluator =
            Substitute.For<IFailurePolicyEvaluator>();

        evaluator
            .Evaluate(
                Arg.Any<ExecutionResult>(),
                Arg.Any<FailurePolicy>())
            .Returns(
                PolicyDecision.Block);

        var transitionEngine =
            Substitute.For<IJobStateTransitionEngine>();

        transitionEngine
            .Apply(
                JobState.Running,
                PolicyDecision.Block)
            .Returns(
                JobState.Blocked);

        var manager =
            new JobStateManager(
                evaluator,
                transitionEngine,
                repository);

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        var result =
            new ExecutionResult(
                ExecutionOutcome.LeaseLost);

        // Act

        await manager.HandleExecutionResultAsync(
            jobKey,
            FailurePolicy.RequireManualReset,
            result);

        // Assert

        await repository
            .Received(1)
            .SetAsync(
                jobKey,
                JobState.Blocked,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CT840_Should_Return_Persisted_State()
    {
        // Arrange

        var repository =
            Substitute.For<IJobStateRepository>();

        repository
            .GetAsync(
                Arg.Any<JobKey>(),
                Arg.Any<CancellationToken>())
            .Returns(
                JobState.Blocked);

        var manager =
            CreateManager(
                repository);

        var jobKey =
            new JobKey(
                "tenant-a",
                "billing",
                "invoice-sync");

        // Act

        var state =
            await manager.GetCurrentStateAsync(
                jobKey);

        // Assert

        Assert.Equal(
            JobState.Blocked,
            state);
    }

    private static JobStateManager CreateManager(
        IJobStateRepository repository)
    {
        return new JobStateManager(
            Substitute.For<IFailurePolicyEvaluator>(),
            Substitute.For<IJobStateTransitionEngine>(),
            repository);
    }
}