# ADR-012: Job State Model

## Status

Accepted

---

## Context

ADR-011 introduced the `ExecutionOutcome` domain model to classify the result of a single execution attempt.

Execution outcomes and job state address different concerns:

- Execution Outcome answers: "How did a specific execution attempt terminate?"
- Job State answers: "Can the job currently be executed?"

As the runtime evolves to support:

- Failure Policies
- Execution History
- Administrative Operations
- Dashboard Capabilities
- Runtime Governance

an explicit job state model is required.

Without a dedicated state model, runtime behavior, failure policies and administrative operations would need to infer execution eligibility from historical execution outcomes, leading to unnecessary complexity and inconsistent interpretations.

The Job State Model was initially explored through the design document:

```text
docs/design/job-state-model-draft.md
```

The draft was used to evaluate the minimum set of states required to support:

- Failure Policies
- Administrative Recovery
- Execution Eligibility
- Future Dashboard Capabilities

The review concluded that a minimal state model consisting of:

- Eligible
- Running
- Blocked

is currently sufficient and avoids introducing unnecessary lifecycle complexity.

This ADR formalizes and adopts the conclusions reached during that design exercise.

---

## Problem Statement

The runtime requires a persistent job state model capable of:

- Representing execution eligibility
- Representing execution activity
- Representing administrative blocking conditions
- Supporting Failure Policies
- Supporting manual recovery workflows
- Remaining independent from execution outcomes

The state model must remain simple and understandable while allowing future runtime capabilities.

---

## Decision

JobGuardian adopts a dedicated Job State Model.

```text
Eligible
Running
Blocked
```

The Job State Model represents the execution eligibility of a job over time.

The state model is intentionally independent from execution outcomes.

---

## State Definitions

### Eligible

The job can be executed.

An eligible job may participate in lease acquisition and execution processes.

This is the default operational state.

---

### Running

The job is currently being executed by a runtime instance.

A running job is expected to own a valid lease, but the state represents execution progress rather than lease ownership.

The state does not imply successful completion.

---

### Blocked

The job cannot be executed.

Blocked represents an operational governance state.

A blocked job may not start new executions until explicitly reset.

The reason why a job is blocked is not represented by the state itself.

---

## Blocking Causes

Blocked represents a single operational state.

Future runtime versions may associate governance metadata with a blocked job, such as:

- Failure Policy enforcement
- Administrative suspension
- Safety quarantine
- Operational lock

These causes shall not introduce additional job states unless future requirements prove the necessity.

---

## State Transition Model

```text
                 +-----------+
                 | Eligible  |
                 +-----------+
                    |     |
                    |     |
                    v     v
             +-----------+ +-----------+
             | Running   | | Blocked   |
             +-----------+ +-----------+
                |     |          |
                |     |          |
                v     v          v
         +-----------+     +-----------+
         | Eligible  |     | Eligible  |
         +-----------+     +-----------+
```

---

## Transition Rules

### Eligible → Running

A lease is successfully acquired and execution starts.

### Running → Eligible

Execution completes and the configured Failure Policy allows future executions.

### Running → Blocked

Execution completes and the configured Failure Policy requires administrative intervention.

### Eligible → Blocked

Administrative action prevents future executions.

Examples:

- Administrative suspension
- Operational lock
- Safety quarantine

### Blocked → Eligible

An administrative reset operation is executed successfully.

---

## Forbidden Transitions

```text
Blocked → Running
```

A blocked job must first return to the Eligible state before execution can start again.

This guarantees explicit operator acknowledgement before execution resumes.

---

## Relationship With ExecutionOutcome

| Concept | Responsibility |
|----------|----------------|
| ExecutionOutcome | Classification of a single execution attempt |
| Job State | Execution eligibility over time |

The same execution outcome may produce different state transitions depending on policy evaluation.

---

## Relationship With Failure Policies

Conceptually:

```text
ExecutionOutcome
        ↓
Failure Policy Evaluation
        ↓
Job State Transition
```

Examples:

```text
Failed
        ↓
Ignore
        ↓
Eligible
```

```text
Failed
        ↓
RequireManualReset
        ↓
Blocked
```

Policy evaluation remains a separate concern.

---

## Relationship With Execution History

Execution History answers:

```text
What happened?
```

Job State answers:

```text
What can happen next?
```

These concepts are intentionally separated.

Job State is not derived from Execution History.

Execution History records execution attempts.

Job State represents the current operational eligibility of a job.

Historical records and current state must remain independent concepts.

---

## Relationship With Observability

Future observability systems should expose both:

- Execution Outcomes
- Job States

Examples:

```text
jobguardian_execution_total{
    outcome="Failed"
}
```

```text
jobguardian_job_state{
    state="Blocked"
}
```

This separation provides a more accurate operational representation of the runtime.

---

## Alternatives Considered

### Reuse ExecutionOutcome As Job State

Rejected.

### No Explicit Job State Model

Rejected.

### Rich State Machine

Rejected.

Examples:

```text
Created
Eligible
Running
Failed
Succeeded
Cancelled
Blocked
Archived
```

This introduces unnecessary complexity at the current maturity stage of the project.

---

## Consequences

### Positive

- Clear separation between outcome and state
- Simplified Failure Policy evaluation
- Explicit administrative recovery workflows
- Easier dashboard integration
- Reduced ambiguity in runtime behavior

### Negative

- Additional runtime concept to maintain
- Future components must manage state transitions explicitly

---

## Related Documents

Design documents:

```text
docs/design/job-state-model-draft.md
```

Related ADRs:

```text
ADR-006 Runtime Architecture

ADR-010 Runtime Scheduling Strategy

ADR-011 Failure Handling and Execution Outcome Model
```

---

## Follow-Up Work

```text
ExecutionResult

Failure Policy Engine

Execution History

Administrative Reset Operations

Dashboard State Visualization

OpenTelemetry Integration
```

Future runtime capabilities shall use the Job State Model defined in this ADR as the authoritative representation of execution eligibility.