# Execution Result Draft

## Goal

Represent the result of a single execution attempt.

ExecutionResult acts as the boundary object between:

- Runtime execution
- ExecutionOutcome
- Future Failure Policy evaluation
- Future Execution History persistence
- Future Observability

ExecutionResult is intended to become the canonical representation of the outcome produced by the execution runtime.

It should provide sufficient information to support future evolution without coupling runtime execution to persistence, policy evaluation, telemetry, or administrative concerns.

---

## Context

ADR-011 introduced the ExecutionOutcome model.

ExecutionOutcome classifies how a specific execution attempt terminated.

Possible outcomes are:

```text
Succeeded
Failed
Cancelled
LeaseLost
Skipped
```

ADR-012 introduced the Job State Model.

ExecutionResult must remain independent from Job State.

ExecutionResult describes an execution attempt.

Job State describes execution eligibility over time.

These concepts must not be mixed.

---

## Proposed Model

Initial proposal:

```csharp
public sealed record ExecutionResult(
    ExecutionOutcome Outcome,
    Exception? Exception = null);
```

ExecutionOutcome remains the authoritative classification.

Exception information is optional and may be attached when an execution outcome originates from an exception.

The model should remain as small as possible until concrete requirements justify additional fields.

---

## Responsibilities

ExecutionResult is responsible for representing:

- execution classification
- execution termination semantics
- execution outcome communication between runtime components
- optional exception information associated with an execution attempt

Examples:

```text
Job completed successfully
    ↓
ExecutionOutcome.Succeeded
    ↓
ExecutionResult
```

```text
Lease acquisition not granted
    ↓
ExecutionOutcome.Skipped
    ↓
ExecutionResult
```

```text
Job throws exception
    ↓
ExecutionOutcome.Failed
    ↓
ExecutionResult
```

---

## Mapping

The runtime shall map execution events according to the following rules.

```text
Successful completion
    ↓
ExecutionOutcome.Succeeded
```

```text
Unhandled exception
    ↓
ExecutionOutcome.Failed
```

```text
Cooperative cancellation
    ↓
ExecutionOutcome.Cancelled
```

```text
Lease ownership lost
    ↓
ExecutionOutcome.LeaseLost
```

```text
Lease acquisition not granted
    ↓
ExecutionOutcome.Skipped
```

---

## Relationship With Failure Policy

ExecutionResult does not perform policy evaluation.

Conceptually:

```text
ExecutionResult
        ↓
Failure Policy Evaluation
        ↓
Job State Transition
```

ExecutionResult provides information.

Failure Policies make decisions.

These responsibilities must remain separate.

Failure Policies may evaluate:

- ExecutionOutcome
- Exception information

when appropriate.

ExecutionResult itself does not contain policy behavior.

---

## Relationship With Exceptions

Some execution outcomes may originate from exceptions.

Examples:

```text
Failed
```

and

```text
Cancelled
```

may carry exception information.

Other outcomes, such as:

```text
Skipped
LeaseLost
Succeeded
```

typically occur without an exception.

Exception information is optional and must not be used as the primary classification mechanism.

ExecutionOutcome remains the authoritative classification.

Consumers must never infer execution outcomes from exception types.

Exception information exists to support:

- Failure Policy evaluation
- Diagnostic workflows
- Future operational analysis

without requiring additional runtime classifications.

---

## Relationship With Job State

ExecutionResult does not represent Job State.

Examples:

```text
ExecutionResult:
    Failed
```

may produce:

```text
Job State:
    Eligible
```

or:

```text
Job State:
    Blocked
```

depending on Failure Policy evaluation.

ExecutionResult and Job State serve different purposes.

---

## Relationship With Execution History

ExecutionResult does not represent Execution History.

ExecutionResult classifies a single execution attempt.

Execution History persists execution attempts over time.

Conceptually:

```text
ExecutionResult
        ↓
Execution History Record
```

Execution History may use ExecutionResult as input, but the two models are distinct.

---

## Relationship With Observability

ExecutionResult may be used as the canonical classification source for:

- metrics
- traces
- logs
- dashboards

Examples:

```text
ExecutionResult:
    LeaseLost

Metric:
    outcome="LeaseLost"
```

ExecutionResult itself should not contain telemetry-specific information.

---

## Non Goals

ExecutionResult must not contain:

- Failure Policy information
- Job State information
- Retry information
- Administrative information
- Dashboard information
- Telemetry metadata
- Persistence metadata
- Scheduling metadata

ExecutionResult should remain a runtime domain concept.

---

## Open Questions

### Q1

Should ExecutionResult contain Exception information?

Current recommendation:

```text
Yes.

Exception information may be required by future Failure Policy evaluation and diagnostic workflows.

Exception remains optional because not all execution outcomes originate from exceptions.
```

---

### Q2

Should ExecutionResult contain timing information?

Examples:

```text
StartedAt
CompletedAt
Duration
```

Current recommendation:

```text
No.

Timing belongs to Execution History and Observability concerns.
```

---

### Q3

Should ExecutionResult contain execution identifiers?

Examples:

```text
ExecutionId
OwnerId
JobKey
```

Current recommendation:

```text
No.

ExecutionResult should remain focused on execution outcome classification.
```

---

## Design Principle

Prefer the smallest ExecutionResult capable of expressing runtime outcome semantics.

ExecutionResult must provide value beyond ExecutionOutcome alone.

Currently, the only additional information considered architecturally justified is optional exception information.

Additional fields should be introduced only when required by a concrete architectural requirement.

Avoid turning ExecutionResult into:

- a general-purpose execution context
- an execution history record
- a telemetry payload
- a policy evaluation object
- a runtime state model