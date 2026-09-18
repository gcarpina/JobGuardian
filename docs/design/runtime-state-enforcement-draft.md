# Runtime State Enforcement Draft

## Goal

Ensure that persisted Job State influences runtime execution behavior.

The purpose of Runtime State Enforcement is to prevent execution of jobs that are not currently eligible for execution.

Conceptually:

```text
Scheduler Tick
        ↓
Job State Check
        ↓
Eligible ?
      ↙       ↘
    No         Yes
     ↓           ↓
   Skip       Execute
```

Runtime State Enforcement transforms persisted Job State into observable runtime behavior.

---

## Context

ADR-012 introduced:

```text
Job State Model

Eligible
Running
Blocked
```

Subsequent iterations introduced:

```text
FailurePolicyEvaluator

PolicyDecision

JobStateTransitionEngine

JobStateRepository

JobStateManager
```

The current architecture can:

- Determine execution outcomes
- Produce policy decisions
- Transition states
- Persist state

The remaining capability is enforcing state during execution scheduling.

---

## Architectural Decision

### Decision

Job State shall be evaluated before every scheduled execution attempt.

### Rationale

Runtime behavior must always reflect the most recent persisted state.

Examples:

```text
Administrator blocks job
```

or

```text
Execution failure blocks job
```

must become effective immediately.

---

## Proposed Runtime Flow

```text
HostedService
        ↓
Load Job State
        ↓
Blocked ?
      ↙       ↘
    Yes       No
     ↓         ↓
   Skip     Execute
                ↓
        JobExecutionCoordinator
                ↓
          ExecutionResult
                ↓
          JobStateManager
```

---

## Runtime Eligibility Evaluation

### Eligible

```text
Eligible
        ↓
Execution allowed
```

---

### Blocked

```text
Blocked
        ↓
Execution denied
```

The execution attempt must be skipped.

---

## Runtime State Resolution

The Hosted Service shall obtain the current state through:

```csharp
IJobStateManager
```

Example:

```csharp
var state =
    await jobStateManager
        .GetCurrentStateAsync(
            jobKey,
            cancellationToken);
```

---

## State Resolution Semantics

### Missing State

Repository:

```text
null
```

Runtime:

```text
Eligible
```

Decision:

```text
Execute
```

---

### Persisted Blocked State

Repository:

```text
Blocked
```

Decision:

```text
Skip
```

---

## Hosted Service Responsibility

The Hosted Service becomes responsible for:

```text
Scheduling

State enforcement

Dependency resolution

Execution triggering
```

The Hosted Service remains unaware of:

```text
Failure policy evaluation

State transitions

State persistence implementation
```

All state-related behavior remains delegated to:

```text
IJobStateManager
```

---

## Execution Flow

### Normal Execution

```text
Tick
        ↓
Eligible
        ↓
Execute
        ↓
ExecutionResult
        ↓
JobStateManager
```

---

### Blocked Job

```text
Tick
        ↓
Blocked
        ↓
Skip
```

No execution occurs.

No lease acquisition occurs.

No coordinator activity occurs.

---

## Lease Efficiency

Runtime State Enforcement occurs before lease acquisition.

Preferred flow:

```text
Tick
        ↓
State Check
        ↓
Eligible ?
        ↓
Lease Acquisition
```

Avoid:

```text
Tick
        ↓
Lease Acquisition
        ↓
State Check
```

Rationale:

Blocked jobs should not consume lease resources.

---

## Observability Requirements

The Hosted Service should log state enforcement decisions.

Example:

```text
Job execution skipped because the job is blocked.
```

Recommended information:

```text
JobKey

Current State
```

---

## Metrics

Future metrics may include:

```text
blocked_execution_attempts_total

blocked_jobs_total

state_transitions_total
```

These metrics are out of scope for the initial implementation.

---

## State Transition Flow

### Failure With RequireManualReset

```text
Eligible
        ↓
Execute
        ↓
Failed
        ↓
Block
        ↓
Blocked
        ↓
Persist
```

---

### Subsequent Tick

```text
Blocked
        ↓
Skip
```

This demonstrates successful runtime enforcement.

---

## Administrative Reset Flow

Future capability:

```text
Blocked
        ↓
Administrative Reset
        ↓
Eligible
```

After reset:

```text
Next Tick
        ↓
Execute
```

Administrative operations remain outside the Hosted Service.

---

## Relationship With JobExecutionCoordinator

JobExecutionCoordinator remains responsible for:

```text
Lease acquisition

Heartbeat management

Execution

ExecutionResult production
```

The coordinator does not:

```text
Read Job State

Persist Job State

Enforce Job State
```

This separation must be preserved.

---

## Relationship With JobStateManager

JobStateManager remains the single entry point for state lifecycle management.

Responsibilities:

```text
State lookup

Policy evaluation

State transition

State persistence
```

Hosted Service interacts only with:

```text
IJobStateManager
```

---

## Security Considerations

Blocked state enforcement must occur on every execution attempt.

State checks must not be cached indefinitely.

Rationale:

Administrative state changes should become visible immediately.

---

## Future Evolution

Future versions may introduce additional states:

```text
Suspended

Quarantined

Disabled
```

Runtime State Enforcement should evolve by evaluating state eligibility rather than enumerating individual states throughout the codebase.

---

## Non Goals

The initial Runtime State Enforcement capability must not introduce:

- Execution History
- Retry Scheduling
- Administrative APIs
- Dashboard Integration
- Audit Log Persistence
- Distributed State Caching

---

## Open Questions

### Q1

Should Blocked jobs generate warnings or informational logs?

Current recommendation:

```text
Information
```

Blocked execution is expected behavior.

---

### Q2

Should blocked jobs acquire leases?

Current recommendation:

```text
No.
```

State enforcement should occur before lease acquisition.

---

### Q3

Should JobStateManager expose a dedicated eligibility method?

Example:

```csharp
Task<bool> IsExecutionAllowedAsync(...)
```

Current recommendation:

```text
No.
```

Keep JobStateManager focused on state.

Eligibility remains a runtime concern.

---

## Design Principle

Prefer:

```text
State
        ↓
Eligibility Decision
        ↓
Execution
```

over:

```text
Execution
        ↓
Detect Blocked State
```

Blocked jobs should be prevented from executing rather than interrupted after execution has already begun.

This minimizes:

- resource consumption
- lease usage
- operational noise
- runtime complexity

while preserving a clear separation between execution and state lifecycle management.