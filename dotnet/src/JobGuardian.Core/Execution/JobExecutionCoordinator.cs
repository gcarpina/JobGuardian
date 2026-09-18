using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Contracts;
using JobGuardian.Core.Models;

namespace JobGuardian.Core.Execution;

public sealed class JobExecutionCoordinator
    : IJobExecutionCoordinator
{
    private readonly ILeaseStore _leaseStore;

    private readonly ILeaseHeartbeatService _heartbeatService;

    public JobExecutionCoordinator(
        ILeaseStore leaseStore,
        ILeaseHeartbeatService heartbeatService)
    {
        _leaseStore = leaseStore;
        _heartbeatService = heartbeatService;
    }

    public async Task<bool> ExecuteAsync(
        ActiveExecution execution,
        JobExecutionOptions options,
        Func<CancellationToken, Task> job,
        CancellationToken cancellationToken = default)
    {
        var acquired =
            await _leaseStore.TryAcquireAsync(
                execution,
                options.LeaseDuration,
                cancellationToken);

        if (!acquired)
        {
            return false;
        }

        using var executionTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        using var heartbeatTokenSource =
            new CancellationTokenSource();

        var heartbeatTask =
            _heartbeatService.RunAsync(
                execution,
                options,
                heartbeatTokenSource.Token);

        try
        {
            var jobTask =
                job(
                    executionTokenSource.Token);

            var completedTask =
                await Task.WhenAny(
                    jobTask,
                    heartbeatTask);

            if (completedTask == heartbeatTask)
            {
                var heartbeatSucceeded =
                    await heartbeatTask;

                if (!heartbeatSucceeded)
                {
                    executionTokenSource.Cancel();

                    try
                    {
                        await jobTask;
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    return false;
                }
            }

            await jobTask;

            return true;
        }
        finally
        {
            heartbeatTokenSource.Cancel();

            try
            {
                await heartbeatTask;
            }
            catch (OperationCanceledException)
            {
            }

            await _leaseStore.ReleaseAsync(
                execution.JobKey,
                execution.ExecutionId,
                cancellationToken);
        }
    }
}