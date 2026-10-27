using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Abstractions.Models;

/// <summary>
/// Defines a paginated execution-history query for one tenant-scoped job.
/// </summary>
public sealed record ExecutionHistoryQuery
{
    /// <summary>
    /// Gets the job whose history is queried.
    /// </summary>
    public required JobKey JobKey { get; init; }

    /// <summary>
    /// Gets the inclusive lower bound for execution start time.
    /// </summary>
    public required DateTimeOffset StartedFromUtc { get; init; }

    /// <summary>
    /// Gets the exclusive upper bound for execution start time.
    /// </summary>
    public required DateTimeOffset StartedBeforeUtc { get; init; }

    /// <summary>
    /// Gets the outcome filter, or <see langword="null"/> for all outcomes.
    /// </summary>
    public ExecutionOutcome? Outcome { get; init; }

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the number of entries per page. Values from 1 through 100 are accepted.
    /// </summary>
    public int PageSize { get; init; } = 50;
}
