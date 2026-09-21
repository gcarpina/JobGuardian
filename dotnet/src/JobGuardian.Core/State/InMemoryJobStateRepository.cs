using System.Collections.Concurrent;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

using JobGuardian.Abstractions.Contracts;

/// <summary>
/// Provides an in-memory implementation of the job state repository for local and test scenarios.
/// </summary>
public sealed class InMemoryJobStateRepository
    : IJobStateRepository
{
    private readonly ConcurrentDictionary<JobKey, JobState> _states =
        new();

    /// <summary>
    /// Gets the current state for the specified job.
    /// </summary>
    /// <param name="jobKey">
    /// The job whose state is requested.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The current state, or <c>null</c> when no persisted state exists.
    /// </returns>
    public Task<JobState?> GetAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default)
    {
        if (_states.TryGetValue(
                jobKey,
                out var state))
        {
            return Task.FromResult<JobState?>(
                state);
        }

        return Task.FromResult<JobState?>(
            null);
    }

    /// <summary>
    /// Persists the current state for the specified job.
    /// </summary>
    /// <param name="jobKey">
    /// The job whose state should be stored.
    /// </param>
    /// <param name="state">
    /// The state to persist.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    public Task SetAsync(
        JobKey jobKey,
        JobState state,
        CancellationToken cancellationToken = default)
    {
        _states[jobKey] =
            state;

        return Task.CompletedTask;
    }
}