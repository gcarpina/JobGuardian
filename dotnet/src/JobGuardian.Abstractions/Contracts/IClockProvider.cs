namespace JobGuardian.Abstractions.Contracts;

/// <summary>
/// Provides UTC time values for JobGuardian components that need consistent timekeeping.
/// </summary>
public interface IClockProvider
{
    /// <summary>
    /// Gets the current UTC timestamp.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The current UTC timestamp.
    /// </returns>
    Task<DateTimeOffset> GetUtcNowAsync(
        CancellationToken cancellationToken = default);
}