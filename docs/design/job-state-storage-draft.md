# Job State Storage Draft

## Goal

Provide persistent storage for Job State information.

The purpose of Job State Storage is to ensure that state transitions survive:

- Application restarts
- Service redeployments
- Instance failures
- Cluster scaling events

Conceptually:

```text
ExecutionResult
        ↓
FailurePolicyEvaluator
        ↓
PolicyDecision
        ↓
JobStateTransitionEngine
        ↓
JobState
        ↓
JobStateRepository
```

---

## Context

ADR-012 introduced the Job State Model:

```text
Eligible
Running
Blocked
```

The Job State Transition Engine determines state transitions.

A persistent storage mechanism is required to ensure that state information remains durable and can be used by future runtime capabilities.

Examples:

- RequireManualReset
- Administrative operations
- Dashboard visualization
- Future scheduling decisions

---

## Architectural Decision

### Decision

Job State shall be persisted.

### Rationale

Blocking a job is a durable business decision.

Example:

```text
Execution failed
        ↓
RequireManualReset
        ↓
Blocked
```

A service restart must not automatically remove the block condition.

Without persistence:

```text
Blocked
        ↓
Restart
        ↓
Eligible
```

which would invalidate the semantics of RequireManualReset.

---

## Ownership Model

### Decision

State is owned by JobKey.

### Rationale

A JobKey uniquely identifies a job instance.

Current model:

```csharp
JobKey
{
    Tenant,
    Domain,
    Name
}
```

Therefore:

```text
tenant-a / billing / invoice-sync
```

may be:

```text
Blocked
```

while:

```text
tenant-b / billing / invoice-sync
```

remains:

```text
Eligible
```

State isolation remains aligned with the execution model.

---

## Storage Model

### Initial Proposal

```csharp
public sealed record JobStateRecord(
    JobKey JobKey,
    JobState State,
    DateTimeOffset UpdatedAt);
```

---

## Stored Information

### Required

```text
JobKey

JobState

UpdatedAt
```

---

### Deferred

The following information is intentionally excluded from V1:

```text
Failure reason

Execution history

Transition reason

Operator identity

Comments

Retry metadata

Lease metadata
```

These concerns belong to future capabilities.

---

## Repository Contract

### Initial Proposal

```csharp
public interface IJobStateRepository
{
    Task<JobState?> GetAsync(
        JobKey jobKey,
        CancellationToken cancellationToken);

    Task SetAsync(
        JobKey jobKey,
        JobState state,
        CancellationToken cancellationToken);
}
```

---

## Repository Semantics

### GetAsync

Returns:

```text
Current persisted state
```

or:

```text
null
```

when the job has no stored state.

---

### SetAsync

Persists the supplied state.

The repository owns:

```text
Insert

Update

Upsert
```

implementation details.

Consumers should not distinguish between them.

---

## Default State Semantics

If no persisted state exists:

```text
Eligible
```

is assumed.

Conceptually:

```text
Missing State
        ↓
Eligible
```

The repository should remain unaware of this rule.

The runtime should apply the default.

---

## Relationship With Job Execution

The repository does not execute jobs.

The repository only stores state.

Example:

```text
ExecutionResult
        ↓
PolicyDecision
        ↓
State Transition
        ↓
Persist State
```

---

## Relationship With Failure Policy Evaluation

The repository does not evaluate policies.

The repository receives already-computed state.

Conceptually:

```text
FailurePolicyEvaluator
        ↓
PolicyDecision
        ↓
JobStateTransitionEngine
        ↓
JobStateRepository
```

---

## Relationship With Administrative Operations

Administrative operations may modify state.

Examples:

```text
Reset Blocked Job

Force Block Job

Administrative Suspend
```

These operations are outside the repository responsibility.

The repository provides persistence only.

---

## Consistency Requirements

### Strong Consistency

Recommended.

When a state update succeeds:

```text
Subsequent reads
```

should observe the updated value.

---

## Idempotency Requirements

The following operation should be safe:

```text
Set(Blocked)

Set(Blocked)

Set(Blocked)
```

Repeated updates must not introduce inconsistent state.

---

## Future Evolution

Future versions may extend:

```csharp
JobStateRecord
```

with:

```text
Transition timestamp

Blocking reason

Administrative metadata

Audit information

History references
```

The repository contract should remain stable.

---

## Non Goals

The initial Job State Storage capability must not introduce:

- Execution History
- Retry Counters
- Scheduler Metadata
- Dashboard Views
- Telemetry Records
- Audit Logs

These capabilities should evolve independently.

---

## Open Questions

### Q1

Should missing state be stored as Eligible?

Current recommendation:

```text
No.
```

Prefer:

```text
Missing State
        ↓
Assume Eligible
```

to reduce storage requirements.

---

### Q2

Should state transitions be audited?

Current recommendation:

```text
Not in V1.
```

Audit and history should be introduced separately.

---

### Q3

Should JobStateRepository return JobStateRecord instead of JobState?

Current recommendation:

```text
Not yet.
```

The simpler API provides lower complexity and supports all currently known requirements.

---

## Design Principle

Keep storage simple.

Prefer:

```text
JobKey
        ↓
JobState
```

over introducing a generic workflow persistence model.

State persistence should remain:

- Durable
- Predictable
- Testable
- Easy to operate
- Easy to evolve

Complexity should be introduced only when required by real runtime capabilities.