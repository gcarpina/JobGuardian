# Coordinator ExecutionResult Mapping

## Goal

Define how `JobExecutionCoordinator` maps runtime execution events to `ExecutionResult`.

This document prepares the migration from:

```csharp
Task<bool>
```

to:

```csharp
Task<ExecutionResult>
```

without introducing Failure Policies, Execution History, Job State evaluation or observability concerns.

The purpose of this document is to establish a deterministic mapping between runtime events and execution results before modifying the coordinator implementation.

---

## Context

ADR-011 introduced the ExecutionOutcome domain model.

ADR-012 introduced the Job State Model.

ExecutionResult represents the result of a single execution attempt.

The coordinator is the primary runtime component responsible for determining execution outcomes.

Future capabilities such as:

- Failure Policy evaluation
- Job State transitions
- Execution History persistence
- Metrics
- OpenTelemetry

shall consume ExecutionResult produced by the coordinator.

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

## Future Evolution

This mapping is expected to remain stable when introducing:

- Failure Policy Engine
- Job State Runtime
- Execution History
- Administrative Operations
- Dashboard
- OpenTelemetry

Future runtime components shall consume ExecutionResult rather than reclassifying execution outcomes independently.

---

## Next Step

Introduce coordinator tests validating:

```text
CT170 Execute_Should_Return_Succeeded_Result

CT171 Execute_Should_Return_Skipped_Result

CT172 Execute_Should_Return_Failed_Result

CT173 Execute_Should_Return_Cancelled_Result

CT174 Execute_Should_Return_LeaseLost_Result
```

and only afterwards migrate:

```csharp
Task<bool>
```

to:

```csharp
Task<ExecutionResult>
```
