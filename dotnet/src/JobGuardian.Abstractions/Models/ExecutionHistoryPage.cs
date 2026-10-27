namespace JobGuardian.Abstractions.Models;

/// <summary>
/// A page of execution history matching a query.
/// </summary>
/// <param name="Items">The entries included in this page.</param>
/// <param name="Page">The one-based page number.</param>
/// <param name="PageSize">The maximum number of entries requested for the page.</param>
/// <param name="TotalCount">The total number of entries matching the query.</param>
public sealed record ExecutionHistoryPage(
    IReadOnlyList<ExecutionHistoryEntry> Items,
    int Page,
    int PageSize,
    long TotalCount);
