# Job State Transition Engine Draft

## Goal

Apply PolicyDecision values to the Job State Model.

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
```

The engine is responsible only for state transitions.

It does not:

- evaluate policies
- execute jobs
- persist state
- perform administrative actions

---

## Context

ADR-012 introduced the Job State Model:

```text
Eligible
Running
Blocked
```

The Failure Policy Engine produces:

```text
Continue
Block
```

A dedicated component is required to translate policy decisions into job state transitions while maintaining a clear separation of responsibilities.

---

## Responsibilities

The Job State Transition Engine is responsible for:

- Applying PolicyDecision values
- Determining the resulting JobState
- Enforcing valid state transitions
- Remaining independent from persistence

The engine is not responsible for:

- Policy evaluation
- State persistence
- Administrative APIs
- Execution History
- Observability

---

## Inputs

### Current Job State

```csharp
JobState
```

Represents the current operational state of the job.

Examples:

```text
Eligible
Running
Blocked
```

---

### Policy Decision

```csharp
PolicyDecision
```

Represents the outcome of Failure Policy evaluation.

Possible values:

```text
Continue
Block
```

---

## Output

```csharp
JobState
```

Represents the next operational state.

---

## Proposed Contract

```csharp
public interface IJobStateTransitionEngine
{
    JobState Apply(
        JobState currentState,
        PolicyDecision decision);
}
```

The contract is synchronous because transition evaluation is deterministic and does not require I/O.

---

## Transition Matrix

### Running + Continue

```text
Running
        ↓
Eligible
```

Rationale:

Execution completed and future executions remain permitted.

---

### Running + Block

```text
Running
        ↓
Blocked
```

Rationale:

Execution completed and policy requires administrative intervention before future executions may occur.

---

## State Transition Table

| Current State | PolicyDecision | Resulting State |
|---------------|----------------|-----------------|
| Running | Continue | Eligible |
| Running | Block | Blocked |

The initial implementation intentionally supports only transitions originating from the Running state.

---

## Administrative Operations

Some transitions are not driven by PolicyDecision.

Example:

```text
Blocked
        ↓
Administrative Reset
        ↓
Eligible
```

This transition must be performed through an administrative operation and not through the Job State Transition Engine.

---

## Invalid Transitions

Examples:

```text
Blocked
        +
Continue
```

must not automatically become:

```text
Eligible
```

Administrative acknowledgement is required.

---

## Relationship With Failure Policy Evaluation

Policy evaluation determines:

```text
What should happen next?
```

State transition determines:

```text
What is the resulting state?
```

These responsibilities must remain separate.

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
```

---

## Relationship With Job State Persistence

The transition engine determines the target state.

Persistence is responsible for storing the state.

Example:

```text
JobStateTransitionEngine
        ↓
Blocked
        ↓
State Repository
        ↓
Persisted State
```

These concerns must remain independent.

---

## Future Evolution

Future runtime versions may introduce additional transition sources.

Examples:

```text
Administrative Reset

Administrative Suspension

Safety Quarantine
```

These capabilities should be modeled explicitly rather than overloading PolicyDecision.

---

## Non Goals

The initial Job State Transition Engine must not introduce:

- persistence
- repository logic
- database access
- retry behavior
- administrative operations
- execution history
- telemetry concerns

---

## Open Questions

### Q1

Should the engine support only Running transitions?

Current recommendation:

```text
Yes.
```

Only completed executions produce PolicyDecision values.

Therefore the transition engine should initially operate on:

```text
Running
```

as the source state.

---

### Q2

Should administrative transitions be represented by PolicyDecision?

Current recommendation:

```text
No.
```

Administrative actions are a separate concern and should use dedicated operations.

---

### Q3

Should invalid transitions throw exceptions?

Current recommendation:

```text
Yes.
```

Invalid state transitions should fail fast to prevent silent corruption of the Job State Model.

---

## Design Principle

Separate:

```text
Policy Evaluation
```

from:

```text
State Transition Application
```

The Failure Policy Engine decides what should happen.

The Job State Transition Engine determines the resulting state.

Keeping these responsibilities separate improves:

- clarity
- testability
- maintainability
- future extensibility