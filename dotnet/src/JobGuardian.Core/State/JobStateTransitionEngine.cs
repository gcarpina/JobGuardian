using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Core.State;

public sealed class JobStateTransitionEngine
    : IJobStateTransitionEngine
{
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