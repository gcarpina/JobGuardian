# ADR-011: Failure Handling and Execution Outcome Model

## Status

Accepted

---

## Context

JobGuardian coordinates distributed job execution using a lease-based ownership model.

The runtime currently supports:

- Lease acquisition
- Lease renewal through heartbeat
- Lease release
- Distributed ownership enforcement
- Cooperative cancellation
- Runtime observability

As the runtime evolves, additional capabilities will be introduced:

- Failure Policies
- Execution History
- Execution Auditing
- Metrics
- OpenTelemetry Integration

These capabilities require a consistent and explicit execution outcome model.

Until now, execution outcomes have been implicitly represented through a combination of:

- Boolean return values
- Exceptions
- Cancellation tokens
- Lease ownership state

This approach is insufficient for future evolution because it does not provide a stable domain language describing the result of an execution attempt.

---

## Problem Statement

The runtime requires a formal execution outcome model capable of:

- Representing all execution termination scenarios
- Providing a shared vocabulary across runtime components
- Supporting future Failure Policies
- Supporting Execution History recording
- Supporting observability and metrics
- Remaining independent from specific persistence implementations

Without an explicit outcome model, future features would duplicate termination logic and risk inconsistencies.

---

## Decision

JobGuardian adopts an explicit `ExecutionOutcome` domain model.

```csharp
public enum ExecutionOutcome
{
    Succeeded,
    Failed,
    Cancelled,
    LeaseLost,
    Skipped
}
```

This enum becomes the authoritative representation of an execution result at the domain level.

---

## Execution Outcome Semantics

### Succeeded

Represents successful execution completion.

Conditions:

- Lease ownership acquired
- Job started
- Job completed successfully
- Lease released normally

Example:

```text
Acquire
↓
Execute
↓
Complete
↓
Release
↓
Succeeded
```

---

### Failed

Represents execution failure caused by an exception.

Conditions:

- Lease ownership acquired
- Job started
- Unhandled exception occurred

Example:

```text
Acquire
↓
Execute
↓
Exception
↓
Failed
```

---

### Cancelled

Represents cooperative execution cancellation.

Conditions:

- Execution started
- Cancellation requested
- Job acknowledged cancellation

Example:

```text
Acquire
↓
Execute
↓
Cancel
↓
Cancelled
```

Typical sources:

- Application shutdown
- External cancellation

---

### LeaseLost

Represents execution interruption caused by lease ownership loss.

Conditions:

- Lease acquired
- Execution started
- Heartbeat renewal failed
- Lease ownership lost

Example:

```text
Acquire
↓
Execute
↓
Lease Lost
↓
LeaseLost
```

This outcome is intentionally distinct from `Failed`.

Lease ownership loss is an infrastructure-level event rather than an application failure.

---

### Skipped

Represents an attempt that did not start the job callback.

Conditions:

- Lease acquisition failed

Example:

```text
Acquire
↓
Not Acquired
↓
Skipped
```

The job was never executed.

---

## Relationship With Job State Model

ExecutionOutcome represents the result of a single execution attempt.

ExecutionOutcome is intentionally limited to attempt classification and does not represent the persistent lifecycle state of a job.

Examples:

- `Succeeded` represents a completed execution attempt
- `Failed` represents a failed execution attempt
- `Skipped` represents an execution attempt that never started

ExecutionOutcome shall not be interpreted as a durable runtime state.

ADR-012 introduces the dedicated Job State Model responsible for representing job eligibility over time:

- Eligible
- Running
- Blocked

The Job State Model and ExecutionOutcome serve different purposes:

| Concept | Purpose |
|----------|----------|
| ExecutionOutcome | Classification of a single execution attempt |
| Job State Model | Persistent lifecycle state of a job over time |

ExecutionOutcome remains focused exclusively on describing how a specific execution attempt terminated.

Failure Policies, Execution History, administrative operations and dashboard views may consume
`ExecutionOutcome` values, but are not represented by `ExecutionOutcome` itself. The current
runtime implements failure-policy evaluation, job-state updates, and a minimal PostgreSQL-backed
execution history. Dashboard capabilities remain outside the MVP.

---

## Outcome Mapping Rules

The runtime shall map execution events according to the following table.

| Runtime Event | ExecutionOutcome |
|---------------|------------------|
| Successful completion | Succeeded |
| Job exception | Failed |
| Cancellation request honoured | Cancelled |
| Lease ownership lost | LeaseLost |
| Lease acquisition not granted | Skipped |

---

## Failure Policy Integration

Execution outcomes are independent from Failure Policies.

Failure Policies consume outcomes rather than infer state directly from runtime behavior.

The current implementation supports `Ignore` and `RequireManualReset`:

```text
Failed
→ Eligible (Ignore)

Failed
→ Blocked (RequireManualReset)
```

ExecutionOutcome does not imply any specific runtime action.

The same outcome may produce different actions depending on the configured Failure Policy.

Retry is not currently implemented. The supported policies determine whether a failure leaves
the job eligible or blocks it:

```text
Outcome       Ignore             RequireManualReset
Failed        Eligible           Blocked
LeaseLost     Eligible           Blocked
```

Failure Policies evaluate outcomes and determine the supported subsequent runtime behavior.

---

## Execution History Integration

Execution history records persist outcomes using `ExecutionOutcome`.

Example:

```text
ExecutionId
JobKey
StartedAt
CompletedAt
ExecutionOutcome
```

History persistence shall not introduce additional outcome categories.

`ExecutionOutcome` remains the single source of truth.

`ExecutionHistory` is responsible for persisting execution attempts over time.

ExecutionOutcome classifies a single execution attempt and does not define how historical records are stored, retained, or queried.

The initial .NET implementation has the following boundaries:

- The hosted runtime creates a history record before lease acquisition and updates it after the
  attempt completes
- `StartedAtUtc` marks the start of the coordination attempt; a `Skipped` outcome means the job
  callback did not run because the lease was unavailable
- An incomplete record indicates that the process stopped or the final update failed before an
  outcome was persisted
- PostgreSQL persistence is enabled by the PostgreSQL provider; runtimes without an
  `IExecutionHistoryStore` log that history is disabled
- History write failures are logged but do not replace the job outcome or prevent execution
- The runtime stores failure categories but does not persist exception messages, which may contain
  sensitive data
- Correlation and conversation identifiers remain unset until the runtime has a propagation
  contract for them
- Timestamps are UTC values from the runtime host; clock skew can affect ordering across hosts
- The initial API reads an execution by ID; list/query APIs and retention automation are not yet
  included

The PostgreSQL history table grows until an operator applies a retention policy. Deployments that
enable history must account for storage growth and define retention before sustained production use.

---

## Observability Integration

Future logging, metrics and tracing systems shall use `ExecutionOutcome` as the canonical classification.

Examples:

```text
jobguardian_execution_total{
    outcome="Succeeded"
}
```

```text
jobguardian_execution_total{
    outcome="LeaseLost"
}
```

This guarantees consistency across:

- Logs
- Metrics
- Traces
- Audit records

---

## Alternatives Considered

### Boolean Success/Failure

Rejected.

```csharp
bool
```

cannot distinguish:

- Failed
- Cancelled
- LeaseLost
- Skipped

---

### Exception-Based Classification

Rejected.

Exceptions do not represent all runtime outcomes.

Examples:

- Skipped
- LeaseLost

may occur without exceptions.

---

### Outcome Inferred From History Records

Rejected.

Execution outcomes must exist independently from persistence implementations.

The domain model should not depend on storage.

---

## Consequences

### Positive

- Explicit domain language
- Consistent failure classification
- Simplified observability
- Foundation for Failure Policies
- Execution History classification and persistence
- Reduced ambiguity across runtime components

### Negative

- Additional domain concept to maintain
- Future runtime components must explicitly map outcomes

---

## Follow-Up Work

Implemented in the current runtime:

```text
ExecutionResult mapping

Failure Policy evaluation

Job State integration
```

Deferred beyond the MVP:

```text
Execution Metrics

OpenTelemetry Integration
```

All future execution-related capabilities shall build upon the `ExecutionOutcome` model defined in this ADR.