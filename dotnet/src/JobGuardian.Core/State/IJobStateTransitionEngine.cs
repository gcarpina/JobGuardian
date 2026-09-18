using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Core.State;

/// <summary>
/// Applies PolicyDecision values to the Job State Model
/// and determines the resulting JobState.
///
/// The engine is responsible only for state transitions.
///
/// It does not:
/// - evaluate policies
/// - persist state
/// - execute jobs
/// - perform administrative operations
/// </summary>
public interface IJobStateTransitionEngine
{
    JobState Apply(
        JobState currentState,
        PolicyDecision decision);
}