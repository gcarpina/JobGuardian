# ADR-010 - Runtime Scheduling Strategy

## Status

Accepted

## Date

2026-08-28

---

## Executive Summary

JobGuardian adopts a **Fixed Delay Scheduling** model for the first version of its runtime.

The primary objective of JobGuardian is to provide reliable distributed job execution through lease coordination, ownership management, heartbeat renewal, and failover handling.

Advanced scheduling capabilities such as Fixed Rate Scheduling, Cron Scheduling, overlap policies, trigger persistence, and misfire handling are intentionally out of scope for the current version.

This decision minimizes implementation and operational complexity while allowing the distributed execution engine to mature before introducing advanced scheduling capabilities.

---

## Context

JobGuardian is designed primarily as a distributed execution framework.

The current runtime architecture is:

```text
HostedService
    ↓
Execution Loop
    ↓
Job Execution Coordinator
    ↓
Lease Store
    ↓
Heartbeat Service
    ↓
Distributed Job Execution
```

The runtime is responsible for:

- Distributed execution coordination
- Lease acquisition and renewal
- Execution ownership
- Failover handling
- Execution lifecycle management

The runtime is currently not responsible for:

- Enterprise scheduling
- Cron expression processing
- Trigger persistence
- Trigger recovery
- Misfire handling
- Calendar-based execution planning

A scheduling strategy is nevertheless required to periodically trigger execution attempts.

---

## Architectural Scope

This ADR applies to:

- Runtime execution loops
- Periodic execution triggering
- HostedService scheduling behaviour
- Runtime polling strategies

This ADR does not apply to:

- Lease management
- Lease ownership
- Heartbeat management
- Distributed coordination
- Retry policies
- Future cron scheduling capabilities
- Future scheduling persistence mechanisms

---

## Assumptions

The following assumptions are considered valid for this decision:

- JobGuardian is primarily a distributed execution framework
- Scheduling precision is less important than execution correctness
- Advanced scheduling capabilities may be implemented in future versions
- External schedulers may be used when advanced scheduling requirements exist
- Distributed execution and scheduling are separate architectural concerns

Examples of external schedulers include:

- Kubernetes CronJob
- Quartz.NET
- Hangfire
- Cloud-native scheduling platforms

---

## Decision

The runtime adopts a **Fixed Delay Scheduling** strategy.

Execution semantics:

```text
Job Execution
      ↓
Job Completion
      ↓
Polling Interval
      ↓
Next Execution
```

The polling interval is applied after completion of the previous execution.

Conceptually:

```text
next_execution =
    previous_execution_end +
    polling_interval
```

The polling interval is configured through:

```csharp
RuntimeOptions.PollingInterval
```

---

## Rationale

### Focus on the Primary Problem

The first version of JobGuardian focuses on solving:

```text
Who executes a job
```

before addressing:

```text
When a job executes
```

Ensuring correct distributed execution is considered more important than providing advanced scheduling capabilities.

### Simplicity

The Fixed Delay model:

- Is easy to understand
- Is easy to implement
- Is easy to test
- Produces predictable behaviour
- Minimizes edge cases

The implementation does not require:

- Trigger persistence
- Scheduling recovery
- Trigger replay
- Misfire handling
- Backlog management

### Operational Robustness

The model behaves predictably in the presence of:

- Long-running jobs
- Runtime restarts
- Node failures
- Lease ownership changes
- Failover events

### Architectural Alignment

The decision is fully aligned with the current runtime architecture and allows the distributed execution model to mature before introducing scheduling complexity.

---

## Alternatives Considered

### Alternative A - Fixed Rate Scheduling

Example:

```text
02:00
02:05
02:10
02:15
```

independently of execution duration.

#### Advantages

- Better temporal precision
- No execution drift
- Suitable for time-sensitive workloads

#### Disadvantages

Requires additional decisions regarding overlapping executions:

- Skip
- Queue
- Parallel execution
- Cancel previous execution

Requires more complex runtime behaviour and operational semantics.

#### Decision

Rejected for the first version.

---

### Alternative B - Cron Scheduling

Example:

```text
0 2 * * *
```

#### Advantages

- Familiar operational model
- Suitable for enterprise batch workloads
- Calendar-based scheduling

#### Disadvantages

Requires additional capabilities:

- Cron parsing
- Trigger persistence
- Trigger recovery
- Misfire handling
- Time zone management

Introduces substantial architectural complexity that is not required for the current project phase.

#### Decision

Rejected for the first version.

---

## Future Evolution

This ADR intentionally limits the scope of the first runtime version.

Future versions may introduce:

### Scheduling Modes

```text
Fixed Delay
Fixed Rate
Cron
```

### Overlap Policies

```text
Skip
Queue
Parallel
Cancel Previous
```

### Additional Capabilities

```text
Cron parsing
Trigger persistence
Misfire handling
Trigger recovery
Calendar-based scheduling
```

The introduction of these capabilities requires dedicated architectural decisions and separate ADRs.

This ADR does not prescribe how future scheduling capabilities must be implemented.

---

## Architectural Principle

Scheduling and distributed execution are separate concerns.

Conceptual model:

```text
Scheduler
        ↓
      Trigger

JobGuardian Runtime
        ↓
Distributed Coordination
        ↓
Lease Management
        ↓
Job Execution
```

This separation:

- Preserves architectural flexibility
- Reduces runtime complexity
- Allows integration with external schedulers
- Supports incremental platform evolution

---

## Verification

The implementation is considered compliant with this ADR if:

- A new execution attempt is triggered only after completion of the previous execution
- The configured polling interval is applied after completion of the previous execution
- The runtime loop itself does not generate overlapping executions
- Runtime scheduling behaviour follows Fixed Delay semantics

---

## Consequences

### Positive

- Simple implementation
- Predictable behaviour
- Reduced operational complexity
- Faster delivery of the distributed execution engine
- Easier testing and troubleshooting
- Clear separation of concerns

### Negative

- Execution drift may occur over time
- No cron scheduling support
- No fixed-rate scheduling
- No overlap management policies
- Limited scheduling flexibility

These limitations are considered acceptable for the current maturity level of the project.

---

## References

Related concepts:

- Runtime execution loop
- Job execution coordinator
- Lease management
- Heartbeat management
- Distributed execution ownership
