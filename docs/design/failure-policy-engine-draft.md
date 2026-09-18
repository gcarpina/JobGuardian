# Failure Policy Engine Draft

## Goal

Define how JobGuardian evaluates execution outcomes and determines the next operational state of a job.

The Failure Policy Engine acts as the decision-making layer between:

- ExecutionResult
- Job State Model

Conceptually:

```text
ExecutionResult
        ↓
Failure Policy Evaluation
        ↓
Policy Decision
        ↓
Job State Transition
```

The engine evaluates outcomes and produces decisions.

It does not execute state transitions directly.

---

## Context

ADR-011 introduced:

```text
ExecutionOutcome
```

ADR-012 introduced:

```text
Job State Model

Eligible
Running
Blocked
```

ExecutionResult now provides the canonical representation of execution termination.

A dedicated policy layer is required to determine how execution outcomes influence future execution eligibility.

---

## Responsibilities

The Failure Policy Engine is responsible for:

- Evaluating ExecutionResult
- Applying configured policy rules
- Producing policy decisions
- Remaining independent from persistence

The Failure Policy Engine is not responsible for:

- Lease management
- Execution coordination
- Job execution
- History persistence
- Observability
- Administrative operations

---

## Inputs

The engine receives:

```csharp
ExecutionResult
```

Example:

```text
Outcome: Failed
Exception: InvalidOperationException
```

or:

```text
Outcome: LeaseLost
Exception: null
```

---

## Outputs

The engine produces a PolicyDecision.

Initial proposal:

```csharp
public enum PolicyDecision
{
    Continue,
    Block
}
```

The output represents a policy decision rather than a runtime action.

It does not represent:

- a scheduling instruction
- an administrative action
- a retry operation

It only expresses how execution eligibility should evolve after policy evaluation.

---

## PolicyDecision Semantics

### Continue

The job remains eligible for future execution.

State transition:

```text
Running
        ↓
Eligible
```

Typical examples:

```text
Succeeded
```

```text
Failed + Ignore
```

```text
LeaseLost + Ignore
```

---

### Block

The job becomes blocked and requires administrative intervention.

State transition:

```text
Running
        ↓
Blocked
```

Typical examples:

```text
Failed + RequireManualReset
```

```text
LeaseLost + RequireManualReset
```

---

## PolicyDecision Mapping

```text
PolicyDecision.Continue
        ↓
JobState.Eligible
```

```text
PolicyDecision.Block
        ↓
JobState.Blocked
```

The mapping is intentionally simple.

Additional decisions should only be introduced when supported by concrete requirements.

---

## Initial Policies

### Ignore

Purpose:

Allow future executions regardless of the execution outcome.

Decision matrix:

```text
Succeeded → Continue

Failed → Continue

Cancelled → Continue

LeaseLost → Continue

Skipped → Continue
```

---

### RequireManualReset

Purpose:

Prevent future executions after selected failure conditions.

Decision matrix:

```text
Succeeded → Continue

Failed → Block

Cancelled → Continue

LeaseLost → Block

Skipped → Continue
```

---

## Relationship With ExecutionResult

ExecutionResult represents facts.

Example:

```text
ExecutionOutcome.Failed
```

The Failure Policy Engine interprets those facts.

The same execution result may produce different policy decisions depending on the configured policy.

Example:

```text
Failed
        ↓
Ignore
        ↓
Continue
```

```text
Failed
        ↓
RequireManualReset
        ↓
Block
```

---

## Relationship With Job State

The Failure Policy Engine does not own state.

It produces decisions that may lead to state transitions.

Example:

```text
ExecutionResult
        ↓
Policy Evaluation
        ↓
Block
        ↓
JobState.Blocked
```

---

## Relationship With Execution History

History records what happened.

Policy evaluation determines what should happen next.

These concerns must remain separate.

---

## Future Retry Policies

Retry-based policies are intentionally out of scope for the initial version.

Examples:

```text
Retry

RetryWithBackoff

RetryUntilSuccess

CircuitBreaker
```

These capabilities require additional concepts:

- Timing
- Persistence
- Scheduling
- Retry counters

The initial Failure Policy Engine intentionally excludes retry behavior.

---

## Non Goals

The initial Failure Policy Engine must not introduce:

- Retry scheduling
- Retry counters
- Retry persistence
- Delayed execution
- Execution history
- Administrative APIs
- Dashboard-specific logic

---

## Open Questions

### Q1

Should all outcomes be evaluated by policy?

Current recommendation:

```text
Yes.

Every ExecutionResult should pass through policy evaluation.
```

---

### Q2

Should LeaseLost be treated as a failure?

Current recommendation:

```text
No.

LeaseLost remains distinct from Failed.

The policy may choose to handle them identically, but they remain separate execution outcomes.
```

---

### Q3

Are two policy decisions sufficient?

Current recommendation:

```text
Yes.

Continue

Block
```

The current Job State Model only supports:

```text
Eligible
Blocked
```

Therefore:

```text
Continue
        ↓
Eligible

Block
        ↓
Blocked
```

Additional policy decisions should only be introduced when a concrete use case requires them.

---

## Design Principle

Keep policy evaluation simple.

Prefer:

```text
ExecutionResult
        ↓
PolicyDecision
```

over introducing a complex workflow engine.

Policy evaluation should remain:

- Deterministic
- Predictable
- Testable
- Independent from infrastructure
- Independent from persistence

Complexity should be introduced only when real requirements justify it.