using JobGuardian.Abstractions.Models;

namespace JobGuardian.Abstractions.Contracts;

/// <summary>
/// Allows the hosting application to supply a correlation ID for a job execution.
/// </summary>
/// <remarks>
/// Return <see langword="null"/> when no host correlation ID is available; JobGuardian then
/// generates one. The selected identifier remains stable across retries for that execution.
/// </remarks>
public interface IExecutionCorrelationIdProvider
{
    /// <summary>
    /// Gets the host correlation ID for the next execution of the specified job.
    /// </summary>
    /// <param name="jobKey">The tenant-scoped identity of the job.</param>
    /// <returns>
    /// The host correlation ID, or <see langword="null"/> when JobGuardian should generate one.
    /// </returns>
    Guid? GetCorrelationId(JobKey jobKey);
}
