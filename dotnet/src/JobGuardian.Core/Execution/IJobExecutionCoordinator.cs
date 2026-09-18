using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Models;

namespace JobGuardian.Core.Execution;

public interface IJobExecutionCoordinator
{
    Task<bool> ExecuteAsync(
        ActiveExecution execution,
        JobExecutionOptions options,
        Func<CancellationToken, Task> job,
        CancellationToken cancellationToken);
}