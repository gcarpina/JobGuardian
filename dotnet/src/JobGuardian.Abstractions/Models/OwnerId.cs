namespace JobGuardian.Abstractions.Models;

/// <summary>
/// Identifies a job owner across environment, application, and instance boundaries.
/// </summary>
/// <remarks>
/// Owner identifiers are used to distinguish the runtime instance that currently owns a lease or is
/// responsible for an execution.
/// </remarks>
/// <param name="Environment">The environment hosting the application instance.</param>
/// <param name="Application">The application that owns the execution.</param>
/// <param name="Instance">The specific application instance.</param>
public sealed record OwnerId(
    string Environment,
    string Application,
    string Instance)
{
    /// <summary>
    /// Formats the owner identifier as an environment/application/instance path.
    /// </summary>
    /// <returns>
    /// A human-readable representation of the owner identity.
    /// </returns>
    public override string ToString()
        => $"{Environment}/{Application}/{Instance}";
}