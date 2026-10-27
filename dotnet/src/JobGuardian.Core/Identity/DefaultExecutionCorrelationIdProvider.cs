using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Identity;

internal sealed class DefaultExecutionCorrelationIdProvider
    : IExecutionCorrelationIdProvider
{
    public Guid? GetCorrelationId(JobKey jobKey)
    {
        return null;
    }
}
