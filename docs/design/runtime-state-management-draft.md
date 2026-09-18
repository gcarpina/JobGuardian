# Runtime State Management Draft

## Goal

Introduce a dedicated runtime component responsible for managing Job State lifecycle.

The component coordinates:

- Failure Policy Evaluation
- State Transitions
- State Persistence

while keeping execution concerns separate from state management concerns.

---

## Context

The current domain model provides:

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

State persistence is provided through:

```text
IJobStateRepository
```

What is still missing is a runtime component capable of orchestrating this workflow.

Without such component, state management logic would need to be implemented inside:

```text
HostedService
```

or:

```text
JobExecutionCoordinator
```

Both alternatives would create unnecessary coupling.

---

## Architectural Decision

### Decision

Introduce:

```csharp
IJobStateManager
```

as the orchestrator of runtime state management.

### Rationale

State management is a separate concern from:

- Job execution
- Lease management
- Heartbeat management

The domain already identifies distinct responsibilities:

```text
ExecutionResult
```

describes:

```text
What happened
```

```text
PolicyDecision
```

describes:

```text
What should happen
```

```text
JobState
```

describes:

```text
What is the resulting eligibility state
```

A dedicated component should coordinate these concerns.

---

## Proposed Runtime Flow

```text
HostedService
        ↓
JobExecutionCoordinator
        ↓
ExecutionResult
        ↓
JobStateManager
                ↓
        FailurePolicyEvaluator
                ↓
        PolicyDecision
                ↓
        JobStateTransitionEngine
                ↓
        JobStateRepository
```

---

## JobStateManager Responsibilities

The JobStateManager is responsible for:

- Reading current state
- Evaluating execution results
- Applying policy decisions
- Computing state transitions
- Persisting state changes

The JobStateManager is not responsible for:

- Executing jobs
- Managing leases
- Heartbeat renewals
- Scheduling
- Administrative actions

---

## Proposed Contract

```csharp
public interface IJobStateManager
{
    Task ProcessExecutionResultAsync(
        JobDescriptor descriptor,
        ExecutionResult result,
        CancellationToken cancellationToken = default);
}
```

---

## Initial Workflow

### Step 1

Receive execution result.

```text
ExecutionResult
```

Example:

```text
Failed
```

---

### Step 2

Load current state.

```text
JobStateRepository.GetAsync()
```

---

### Step 3

Determine policy decision.

```text
FailurePolicyEvaluator
```

Example:

```text
Failed
        ↓
RequireManualReset
        ↓
Block
```

---

### Step 4

Apply state transition.

```text
Running
        ↓
Block
        ↓
Blocked
```

---

### Step 5

Persist new state.

```text
JobStateRepository.SetAsync()
```

---

## Runtime Execution Flow

### Successful Execution

```text
Eligible
        ↓
Execute
        ↓
Succeeded
        ↓
Continue
        ↓
Eligible
```

No persist operation is required.

---

### Failure With Ignore

```text
Eligible
        ↓
Execute
        ↓
Failed
        ↓
Continue
        ↓
Eligible
```

No persist operation is required.

---

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

## State Lookup Flow

Before starting a new execution:

```text
HostedService
        ↓
JobStateRepository.GetAsync()
        ↓
State?
```

Decision:

```text
Blocked
        ↓
Skip execution
```

```text
null
        ↓
Assume Eligible
```

```text
Eligible
        ↓
Execute
```

---

## Default State Semantics

The repository does not store default states.

Conceptually:

```text
Missing Record
        ↓
Eligible
```

The runtime owns this rule.

---

## Persistence Rules

### Persist

Persist only:

```text
Blocked
```

---

### Do Not Persist

Do not persist:

```text
Eligible
```

Rationale:

```text
Eligible
```

is the default runtime state.

Persisting it would provide no additional information while increasing storage usage.

---

## Repository Optimization

Preferred model:

```text
No Record
        ↓
Eligible
```

```text
Record Exists
        ↓
Blocked
```

This keeps storage minimal and naturally aligned with the business meaning of persistence.

---

## Administrative Operations

Administrative actions are outside JobStateManager responsibilities.

Examples:

```text
Reset Blocked Job
```

```text
Force Block Job
```

```text
Administrative Suspension
```

These capabilities should be introduced through dedicated application services.

---

## Future Evolution

The JobStateManager provides a natural integration point for future capabilities:

```text
Execution History

Audit Trail

Retry Policies

Administrative Reset

Dashboard Integration
```

without introducing additional coupling in the execution layer.

---

## Relationship With JobExecutionCoordinator

JobExecutionCoordinator remains responsible for:

```text
Lease acquisition

Heartbeat monitoring

Job execution

ExecutionResult generation
```

It must not:

- Evaluate policies
- Manage state
- Persist runtime state

This separation should be preserved.

---

## Relationship With HostedService

HostedService remains responsible for:

```text
Scheduling

Dependency resolution

Execution triggering
```

State management is delegated to:

```text
JobStateManager
```

---

## Open Questions

### Q1

Should current state be loaded before every execution?

Current recommendation:

```text
Yes.
```

This guarantees immediate visibility of administrative state changes.

---

### Q2

Should Eligible state ever be persisted?

Current recommendation:

```text
No.
```

Eligible should remain the implicit default state.

---

### Q3

Should JobStateManager remove persisted records when state becomes Eligible?

Current recommendation:

```text
Yes.
```

Conceptually:

```text
Blocked
        ↓
Eligible
        ↓
Delete Record
```

This preserves the principle:

```text
Missing Record
        ↓
Eligible
```

and keeps storage minimal.

---

## Design Principle

Separate:

```text
Execution
```

from:

```text
State Lifecycle
```

Execution components generate execution facts.

State components evaluate, transition and persist runtime state.

The resulting architecture remains:

- cohesive
- testable
- maintainable
- evolvable