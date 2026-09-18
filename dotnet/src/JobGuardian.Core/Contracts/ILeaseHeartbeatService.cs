using JobGuardian.Abstractions.Models;

using JobGuardian.Core.Models;

namespace JobGuardian.Core.Contracts;

public interface ILeaseHeartbeatService
{
    Task<bool> RunAsync(
        ActiveExecution execution,
        JobExecutionOptions options,
        CancellationToken cancellationToken = default);
}