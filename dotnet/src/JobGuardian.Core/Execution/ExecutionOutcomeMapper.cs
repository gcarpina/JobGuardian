using JobGuardian.Abstractions.Enums;

namespace JobGuardian.Core.Execution;

internal static class ExecutionOutcomeMapper
{
    public static ExecutionOutcome FromSuccess()
    {
        return ExecutionOutcome.Succeeded;
    }

    public static ExecutionOutcome FromFailure()
    {
        return ExecutionOutcome.Failed;
    }

    public static ExecutionOutcome FromCancellation()
    {
        return ExecutionOutcome.Cancelled;
    }

    public static ExecutionOutcome FromLeaseLost()
    {
        return ExecutionOutcome.LeaseLost;
    }

    public static ExecutionOutcome FromLeaseNotAcquired()
    {
        return ExecutionOutcome.Skipped;
    }
}