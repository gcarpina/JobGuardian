namespace JobGuardian.Abstractions.Models;

public sealed record ActiveExecution
{
    public required JobKey JobKey { get; init; }

    public required Guid ExecutionId { get; init; }

    public required string OwnerId { get; init; }
}