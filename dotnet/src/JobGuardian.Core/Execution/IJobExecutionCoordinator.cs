using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Models;

namespace JobGuardian.Core.Execution;

/// <summary>
/// Executes a job while coordinating lease acquisition, heartbeats, and completion.
/// </summary>
/// <remarks>
/// JobGuardian uses this coordinator to execute work under a distributed lease so that only the
/// current active owner continues processing.
/// </remarks>
public interface IJobExecutionCoordinator
{
    /// <summary>
    /// Executes a job with the supplied lease and execution options.
    /// </summary>
    /// <param name="execution">
    /// Information describing the execution request and current owner.
    /// </param>
    /// <param name="options">
    /// The lease and heartbeat settings for the execution.
    /// </param>
    /// <param name="job">
    /// The callback that executes the underlying job logic.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the execution.
    /// </param>
    /// <returns>
    /// <c>true</c> when the job completes successfully under an active lease; otherwise, <c>false</c>
    /// when the lease cannot be maintained or is not acquired.
    /// </returns>
    Task<bool> ExecuteAsync(
        ActiveExecution execution,
        JobExecutionOptions options,
        Func<CancellationToken, Task> job,
        CancellationToken cancellationToken);
}