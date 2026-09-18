using System.Collections.Concurrent;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

using JobGuardian.Abstractions.Contracts;

public sealed class InMemoryJobStateRepository
    : IJobStateRepository
{
    private readonly ConcurrentDictionary<JobKey, JobState> _states =
        new();

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