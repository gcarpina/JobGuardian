using System.Diagnostics;
using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;

using JobGuardian.Core.Contracts;
using JobGuardian.Core.Models;
using JobGuardian.Core.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobGuardian.Core.Execution;

/// <summary>
/// Keeps a lease alive while the owning execution continues to process work.
/// </summary>
public sealed class LeaseHeartbeatService
    : ILeaseHeartbeatService
{
    private readonly ILeaseStore _leaseStore;

    private readonly ILogger<LeaseHeartbeatService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeaseHeartbeatService"/> class.
    /// </summary>
    /// <param name="leaseStore">
    /// The store used to renew the active lease.
    /// </param>
    public LeaseHeartbeatService(
        ILeaseStore leaseStore)
        : this(
            leaseStore,
            NullLogger<LeaseHeartbeatService>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LeaseHeartbeatService"/> class.
    /// </summary>
    /// <param name="leaseStore">
    /// The store used to renew the active lease.
    /// </param>
    /// <param name="logger">
    /// The logger used to report successful renewals at trace level.
    /// </param>
    public LeaseHeartbeatService(
        ILeaseStore leaseStore,
        ILogger<LeaseHeartbeatService> logger)
    {
        _leaseStore = leaseStore;
        _logger = logger;
    }

    /// <summary>
    /// Runs the lease renewal loop for the active execution.
    /// </summary>
    /// <param name="execution">
    /// The execution whose lease should remain active.
    /// </param>
    /// <param name="options">
    /// The lease and heartbeat settings used to renew the lease.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the heartbeat loop.
    /// </param>
    /// <returns>
    /// <c>true</c> when the heartbeat loop exits normally; otherwise, <c>false</c> when the lease
    /// can no longer be renewed.
    /// </returns>
    public async Task<bool> RunAsync(
        ActiveExecution execution,
        JobExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var startedAt =
                Stopwatch.GetTimestamp();

            var duration =
                TimeSpan.Zero;

            var result = "error";

            using var activity =
                JobGuardianTelemetry.StartLeaseActivity(
                    "Renew",
                    execution);

            bool renewed;

            try
            {
                renewed =
                    await _leaseStore.RenewAsync(
                        execution.JobKey,
                        execution.ExecutionId,
                        options.LeaseDuration,
                        cancellationToken);

                result =
                    renewed
                        ? "renewed"
                        : "not_owner";

                activity?.SetTag(
                    "jobguardian.lease.result",
                    result);

                if (!renewed)
                {
                    activity?.SetStatus(
                        ActivityStatusCode.Error);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                result = "cancelled";
                activity?.SetTag(
                    "jobguardian.lease.result",
                    result);
                throw;
            }
            catch (Exception exception)
            {
                activity?.SetTag(
                    "jobguardian.lease.result",
                    result);
                activity?.SetTag(
                    "error.type",
                    exception.GetType().FullName);
                activity?.SetStatus(
                    ActivityStatusCode.Error);
                throw;
            }
            finally
            {
                duration =
                    Stopwatch.GetElapsedTime(startedAt);

                JobGuardianTelemetry.LeaseOperationCompleted(
                    "renew",
                    result,
                    duration.TotalSeconds);
            }

            if (!renewed)
            {
                return false;
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(
                    "Lease heartbeat renewed for job {JobKey} (execution ID {ExecutionId}) in {DurationMilliseconds} ms",
                    execution.JobKey,
                    execution.ExecutionId,
                    duration.TotalMilliseconds);
            }

            try
            {
                await Task.Delay(
                    options.HeartbeatInterval,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return true;
    }
}
