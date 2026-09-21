using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Core.State;

/// <summary>
/// Applies PolicyDecision values to the Job State Model and determines the resulting JobState.
/// </summary>
/// <remarks>
/// The engine is responsible only for state transitions. It does not evaluate policies, persist
/// state, or execute business logic.
/// </remarks>
public interface IJobStateTransitionEngine
{
    /// <summary>
    /// Applies the supplied policy decision to the current state.
    /// </summary>
    /// <param name="currentState">
    /// The current job state.
    /// </param>
    /// <param name="decision">
    /// The decision that should be applied to the state.
    /// </param>
    /// <returns>
    /// The resulting job state after the decision is applied.
    /// </returns>
    JobState Apply(
        JobState currentState,
        PolicyDecision decision);
}