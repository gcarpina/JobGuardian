using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Abstractions.Contracts;

public interface IJobStateRepository
{
    Task<JobState?> GetAsync(
        JobKey jobKey,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        JobKey jobKey,
        JobState state,
        CancellationToken cancellationToken = default);
}