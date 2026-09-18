namespace JobGuardian.Core.Results;

public sealed record AcquireResult
{
    public bool Success { get; init; }

    public Guid ExecutionId { get; init; }

    public string? Reason { get; init; }

    public static AcquireResult Acquired(Guid executionId)
        => new()
        {
            Success = true,
            ExecutionId = executionId
        };

    public static AcquireResult Failed(string reason)
        => new()
        {
            Success = false,
            Reason = reason
        };
}