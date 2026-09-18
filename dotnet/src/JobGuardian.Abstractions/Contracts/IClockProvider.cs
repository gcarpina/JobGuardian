namespace JobGuardian.Abstractions.Contracts;

public interface IClockProvider
{
    Task<DateTimeOffset> GetUtcNowAsync(
        CancellationToken cancellationToken = default);
}