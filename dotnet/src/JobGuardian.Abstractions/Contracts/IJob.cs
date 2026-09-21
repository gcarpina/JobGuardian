namespace JobGuardian.Abstractions.Contracts;

/// <summary>
/// Represents a distributed job that JobGuardian can execute.
/// </summary>
/// <remarks>
/// Implementations define the actual business logic for a coordinated job while JobGuardian
/// manages lease acquisition, heartbeat maintenance, and execution state transitions.
/// </remarks>
public interface IJob
{
    /// <summary>
    /// Executes the job logic for the current owner.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel the execution.
    /// </param>
    Task ExecuteAsync(
        CancellationToken cancellationToken);
}