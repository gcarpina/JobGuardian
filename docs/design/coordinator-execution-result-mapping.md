# Coordinator ExecutionResult Mapping

## Goal

Define how `JobExecutionCoordinator` maps runtime execution events to `ExecutionResult`.

This document records the migration from:

```csharp
Task<bool>
```

to:

```csharp
Task<ExecutionResult>
```

and its integration with job state and failure policy evaluation.

The coordinator classifies execution attempts. The hosted service applies the configured failure
policy to the result and persists the resulting job state.

---

## Context

ADR-011 introduced the ExecutionOutcome domain model.

ADR-012 introduced the Job State Model.

ExecutionResult represents the result of a single execution attempt.

The coordinator is the primary runtime component responsible for determining execution outcomes.

Additional capabilities remain separate:

- Execution History persistence
- Metrics
- OpenTelemetry

These capabilities may consume `ExecutionResult` produced by the coordinator when implemented.

---

## Mapping Principles

The coordinator is responsible for:

- lease acquisition
- lease release
- heartbeat supervision
- execution lifecycle coordination

The coordinator is not responsible for:

- policy evaluation
- state transitions
- history persistence
- observability decisions

The coordinator shall only classify execution results.

---

## Lease Not Acquired

### Scenario

A lease acquisition attempt is rejected because another owner currently owns the job.

### Mapping

```csharp
new ExecutionResult(
    ExecutionOutcome.Skipped);
```

### Rationale

The execution never started.

No failure occurred.

The job was intentionally skipped because ownership could not be obtained.

---

## Job Completed Successfully

### Scenario

The lease is acquired and the job completes normally.

### Mapping

```csharp
new ExecutionResult(
    ExecutionOutcome.Succeeded);
```

### Rationale

Execution completed successfully and all coordination responsibilities were satisfied.

---

## Job Failed

### Scenario

The lease is acquired and the job throws an exception.

### Mapping

```csharp
new ExecutionResult(
    ExecutionOutcome.Failed,
    exception);
```

### Rationale

The execution terminated because of an application-level failure.

Exception information is preserved for future policy evaluation and diagnostics.

---

## External Cancellation

### Scenario

Cancellation is requested by the hosting environment.

Examples:

- Service shutdown
- Application shutdown
- External cancellation request

### Mapping

```csharp
new ExecutionResult(
    ExecutionOutcome.Cancelled,
    exception);
```

### Rationale

The execution was explicitly cancelled.

Cancellation is treated as a distinct outcome rather than a failure.

---

## Lease Lost

### Scenario

Lease ownership is lost during execution.

Examples:

- Heartbeat renewal failure
- Lease ownership expiration
- Ownership transferred to another runtime instance

### Mapping

```csharp
new ExecutionResult(
    ExecutionOutcome.LeaseLost);
```

### Rationale

Lease loss is an infrastructure-level event.

It is intentionally distinguished from application failures.

---

## Outcome Matrix

| Runtime Event | ExecutionOutcome | Exception |
|---------------|------------------|-----------|
| Lease not acquired | Skipped | No |
| Job completed | Succeeded | No |
| Job failed | Failed | Yes |
| Cancellation requested | Cancelled | Optional |
| Lease ownership lost | LeaseLost | No |

---

## Relationship With Failure Policies

ExecutionResult does not evaluate policies.

Conceptually:

```text
ExecutionResult
        ↓
Failure Policy
        ↓
Job State Transition
```

The coordinator remains unaware of policy behavior.

---

## Relationship With Job State

ExecutionResult does not represent Job State.

Examples:

```text
ExecutionResult:
    Failed
```

may later produce:

```text
Job State:
    Eligible
```

or:

```text
Job State:
    Blocked
```

depending on policy evaluation.

---

## Relationship With Execution History

ExecutionResult is an input to Execution History.

Conceptually:

```text
ExecutionResult
        ↓
Execution History Record
```

The coordinator does not create history records.

---

## Implemented Behavior

`IJobExecutionCoordinator.ExecuteAsync` returns `ExecutionResult` with these outcomes:

- lease not acquired: `Skipped`
- job completed: `Succeeded`
- job threw an exception: `Failed`
- host cancellation: `Cancelled`
- heartbeat or lease release indicates ownership loss: `LeaseLost`

The hosted service passes non-skipped outcomes to `IJobStateManager` with the job's configured
`FailurePolicy`. `Skipped` does not change the current state because another owner may be executing
the job.

With `RequireManualReset`, `Failed` and `LeaseLost` block the job until `ResetAsync` succeeds.
With `Ignore`, those outcomes leave the job eligible for its next execution attempt.

## Verification

Coordinator unit tests cover outcome mapping and lease cleanup. Core and PostgreSQL execution-state
flow tests verify blocking after failure, skipping blocked jobs, and resuming after manual reset.
