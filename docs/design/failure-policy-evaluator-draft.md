# Failure Policy Evaluator Draft

## Goal

Evaluate an ExecutionResult according to a configured Failure Policy and produce a PolicyDecision.

Conceptually:

```text
ExecutionResult
        ↓
Failure Policy Evaluator
        ↓
PolicyDecision
```

The evaluator is responsible only for decision-making.

It does not:

- Persist data
- Mutate Job State
- Schedule executions
- Trigger retries
- Perform administrative operations

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

ExecutionResult is now the canonical representation of execution termination.

A dedicated evaluation layer is required to interpret execution outcomes and determine how future execution eligibility should evolve.

---

## Responsibility

The Failure Policy Evaluator is responsible for:

- Evaluating ExecutionResult
- Applying configured Failure Policies
- Producing Policy Decisions

The evaluator is intentionally independent from:

- Runtime execution
- Persistence
- Observability
- Administrative workflows
- Job State storage

---

## Inputs

### ExecutionResult

```csharp
ExecutionResult
```

Examples:

```text
Succeeded
```

```text
Failed
```

```text
LeaseLost
```

---

### JobFailurePolicy

```csharp
JobFailurePolicy
```

Initial policies:

```text
Ignore

RequireManualReset
```

---

## Output

### PolicyDecision

Initial proposal:

```csharp
public enum JobGuardian.Abstractions.Enums.PolicyDecision
{
    Continue,
    Block
}
```

The decision expresses how execution eligibility should evolve.

It does not prescribe runtime actions.

---

## PolicyDecision Semantics

### Continue

The job remains eligible for future execution.

Conceptually:

```text
Running
        ↓
Eligible
```

Examples:

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

Conceptually:

```text
Running
        ↓
Blocked
```

Examples:

```text
Failed + RequireManualReset
```

```text
LeaseLost + RequireManualReset
```

---

## Proposed Contract

```csharp
public interface IFailurePolicyEvaluator
{
    PolicyDecision Evaluate(
        ExecutionResult result,
        JobFailurePolicy policy);
}
```

The contract is synchronous because policy evaluation is purely deterministic and does not require I/O operations.

---

## Evaluation Matrix

### Ignore Policy

```text
Succeeded
        ↓
Continue

Failed
        ↓
Continue

Cancelled
        ↓
Continue

LeaseLost
        ↓
Continue

Skipped
        ↓
Continue
```

---

### RequireManualReset Policy

```text
Succeeded
        ↓
Continue

Failed
        ↓
Block

Cancelled
        ↓
Continue

LeaseLost
        ↓
Block

Skipped
        ↓
Continue
```

---

## Relationship With ExecutionResult

ExecutionResult represents execution facts.

Example:

```text
ExecutionOutcome.Failed
```

The evaluator interprets those facts according to policy configuration.

The same ExecutionResult may produce different decisions depending on the selected policy.

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

The evaluator does not own Job State.

It only produces decisions.

Conceptually:

```text
ExecutionResult
        ↓
Failure Policy Evaluation
        ↓
PolicyDecision
        ↓
Job State Transition
```

A separate component should be responsible for applying state transitions.

---

## Relationship With Execution History

Execution History records what happened.

Failure Policy evaluation determines what should happen next.

These concerns must remain independent.

---

## Future Retry Policies

Retry-oriented policies are intentionally excluded from the initial design.

Examples:

```text
Retry

RetryWithBackoff

RetryUntilSuccess

CircuitBreaker
```

These capabilities introduce additional concerns:

- Scheduling
- Timing
- Persistence
- Retry Counters
- Operational Visibility

The initial evaluator intentionally avoids those responsibilities.

---

## Non Goals

The initial version must not introduce:

- Retry scheduling
- Delayed execution
- Retry counters
- Administrative APIs
- State persistence
- History persistence
- Dashboard concerns
- Telemetry concerns

---

## Open Questions

### Q1

Should every ExecutionResult be evaluated?

Current recommendation:

```text
Yes.
```

Every execution outcome should pass through policy evaluation.

---

### Q2

Should LeaseLost be treated as Failed?

Current recommendation:

```text
No.
```

LeaseLost remains a distinct execution outcome.

Policies may choose to handle both identically, but the domain model should preserve the distinction.

---

### Q3

Are two PolicyDecision values sufficient?

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

Therefore the decision model remains intentionally minimal.

---

## Design Principle

Keep policy evaluation deterministic and simple.

Prefer:

```text
ExecutionResult
        ↓
PolicyDecision
```

over a configurable workflow engine.

Complexity should be introduced only when justified by concrete runtime requirements.