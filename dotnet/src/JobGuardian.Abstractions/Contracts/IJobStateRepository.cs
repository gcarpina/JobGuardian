using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Abstractions.Contracts;

/// <summary>
/// Stores and retrieves the operational state of a job key.
/// </summary>
/// <remarks>
/// JobGuardian uses this repository to persist the current job state for leader election,
/// execution eligibility, and failure handling across application instances.
/// </remarks>
public interface IJobStateRepository
{
    /// <summary>
    /// Gets the current state for the specified job.
    /// </summary>
    /// <param name="jobKey">
    /// The logical identity of the job whose state is requested.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The current state, or <c>null</c> when no persisted state exists.
    /// </returns>
    Task<JobState?> GetAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the current state for the specified job.
    /// </summary>
    /// <param name="jobKey">
    /// The logical identity of the job whose state should be persisted.
    /// </param>
    /// <param name="state">
    /// The state to persist.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    Task SetAsync(
        JobKey jobKey,
        JobState state,
        CancellationToken cancellationToken = default);
}