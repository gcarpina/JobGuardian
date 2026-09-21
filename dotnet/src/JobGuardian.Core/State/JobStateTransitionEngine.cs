using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Core.State;

/// <summary>
/// Applies a policy decision to the running state of a job.
/// </summary>
public sealed class JobStateTransitionEngine
    : IJobStateTransitionEngine
{
    /// <summary>
    /// Applies the supplied policy decision to the current state.
    /// </summary>
    /// <param name="currentState">
    /// The current job state. This must be <see cref="JobState.Running"/> for a valid transition.
    /// </param>
    /// <param name="decision">
    /// The decision that should be applied.
    /// </param>
    /// <returns>
    /// The resulting state after the decision is applied.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the transition is applied from a state other than <see cref="JobState.Running"/>.
    /// </exception>
    public JobState Apply(
        JobState currentState,
        PolicyDecision decision)
    {
        if (currentState != JobState.Running)
        {
            throw new InvalidOperationException(
                $"Cannot apply policy decision '{decision}' from state '{currentState}'.");
        }

        return decision switch
        {
            PolicyDecision.Continue =>
                JobState.Eligible,

            PolicyDecision.Block =>
                JobState.Blocked,

            _ => throw new ArgumentOutOfRangeException(
                nameof(decision),
                decision,
                "Unsupported policy decision.")
        };
    }
}