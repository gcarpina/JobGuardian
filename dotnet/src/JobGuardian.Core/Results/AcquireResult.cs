namespace JobGuardian.Core.Results;

/// <summary>
/// Represents the result of attempting to acquire a job lease.
/// </summary>
public sealed record AcquireResult
{
    /// <summary>
    /// Gets a value indicating whether the lease was acquired successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the execution identifier associated with the acquisition attempt.
    /// </summary>
    public Guid ExecutionId { get; init; }

    /// <summary>
    /// Gets the failure reason when the lease could not be acquired.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Creates a successful lease acquisition result.
    /// </summary>
    /// <param name="executionId">
    /// The execution identifier associated with the acquired lease.
    /// </param>
    /// <returns>
    /// A result indicating that the lease was acquired.
    /// </returns>
    public static AcquireResult Acquired(Guid executionId)
        => new()
        {
            Success = true,
            ExecutionId = executionId
        };

    /// <summary>
    /// Creates a failed lease acquisition result.
    /// </summary>
    /// <param name="reason">
    /// The reason the lease could not be acquired.
    /// </param>
    /// <returns>
    /// A result indicating that the lease acquisition failed.
    /// </returns>
    public static AcquireResult Failed(string reason)
        => new()
        {
            Success = false,
            Reason = reason
        };
}