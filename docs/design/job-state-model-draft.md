# Job State Model Draft

## Goal

Define a minimal state model representing the operational eligibility of a job.

This model is intentionally separate from:

- ExecutionOutcome
- FailurePolicy
- ExecutionHistory
- Observability

The purpose of the model is to answer the question:

```text
Can this job be executed now?
```

---

## States

### Eligible

The job can be executed.

This is the normal operating state.

An eligible job may participate in lease acquisition and execution.

---

### Running

The job is currently being executed by a runtime instance.

A running job is expected to own a valid lease, but the state represents execution progress rather than lease ownership.

Running does not imply successful completion.

---

### Blocked

The job cannot be executed.

Blocked represents an operational governance state.

A blocked job must not start new executions until explicitly unblocked.

The reason why a job is blocked is intentionally separated from the state itself.

Examples include:

- Failure policy enforcement
- Administrative suspension
- Operational lock
- Safety quarantine

---

## Blocking Causes

Blocked represents a single operational state.

The reason why a job is blocked is not part of the Job State Model.

Future runtime versions may associate governance metadata with a blocked job, such as:

- Failure Policy enforcement
- Administrative suspension
- Safety quarantine
- Operational lock

These causes shall not introduce additional job states unless future requirements prove the necessity.

---

## Transitions

### Eligible → Running

Execution starts successfully.

The runtime acquired lease ownership and began execution.

---

### Running → Eligible

Execution completes and the configured Failure Policy allows future executions.

Examples:

```text
Succeeded + Ignore
Failed + Ignore
Cancelled + Ignore
LeaseLost + Ignore
```

---

### Running → Blocked

Execution completes and the configured Failure Policy requires manual intervention.

Examples:

```text
Failed + RequireManualReset
LeaseLost + RequireManualReset
```

---

### Eligible → Blocked

Administrative action prevents future executions.

Examples:

```text
Administrative suspension
Safety quarantine
Operational lock
```

---

### Blocked → Eligible

Administrative reset executed successfully.

---

## Forbidden Transitions

### Blocked → Running

Not allowed.

A blocked job must first become eligible before a new execution can start.

This guarantees explicit operator acknowledgement before execution resumes.

---

## State Diagram

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

## Relationship With ExecutionOutcome

ExecutionOutcome answers:

```text
How did a specific execution attempt terminate?
```

Examples:

```text
Succeeded
Failed
Cancelled
LeaseLost
Skipped
```

Job State answers:

```text
Can the job currently be executed?
```

Examples:

```text
Eligible
Running
Blocked
```

ExecutionOutcome and Job State are intentionally independent concepts.

---

## Examples

### Successful Execution

```text
Eligible
↓
Running
↓
Eligible

Outcome: Succeeded
```

---

### Failure With Ignore Policy

```text
Eligible
↓
Running
↓
Eligible

Outcome: Failed
```

---

### Failure With RequireManualReset Policy

```text
Eligible
↓
Running
↓
Blocked

Outcome: Failed
```

---

### Manual Reset

```text
Blocked
↓
Eligible
```

---

### Lease Loss With Ignore Policy

```text
Eligible
↓
Running
↓
Eligible

Outcome: LeaseLost
```

---

### Lease Loss With RequireManualReset Policy

```text
Eligible
↓
Running
↓
Blocked

Outcome: LeaseLost
```

---

## Open Questions

### Q1

Should Job State remain purely runtime-derived or become persistent?

Current recommendation:

```text
Keep state conceptual until FailurePolicy and ExecutionHistory are designed.
```

---

### Q2

Should administrative operations act on Job State directly?

Current recommendation:

```text
Yes.

Administrative actions should drive state transitions rather than manipulating execution outcomes.
```

---

### Q3

Are additional states necessary?

Current recommendation:

```text
No.

Maintain:

- Eligible
- Running
- Blocked
```

until a concrete use case proves otherwise.