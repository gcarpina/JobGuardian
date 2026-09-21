namespace JobGuardian.Abstractions.Contracts;

/// <summary>
/// Provides the identity used to attribute a distributed job execution to a specific owner.
/// </summary>
/// <remarks>
/// JobGuardian uses this owner identity to distinguish execution ownership across application
/// instances and environments while coordinating lease acquisition and renewal.
/// </remarks>
public interface IExecutionIdentityProvider
{
    /// <summary>
    /// Gets the owner identifier for the current runtime instance.
    /// </summary>
    /// <returns>
    /// A stable identity that uniquely identifies the owner of an execution.
    /// </returns>
    string GetOwnerId();
}