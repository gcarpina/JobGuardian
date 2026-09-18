# ADR-008 - Atomic Lease Acquisition

## Status

Accepted

---

## Context

JobGuardian guarantees singleton execution across multiple processes, services, containers and clusters.

Multiple execution instances may attempt to acquire the same job simultaneously.

Example:

```text
default/finance/nightly-import
```

Possible contenders:

```text
instance-a
instance-b
instance-c
instance-d
```

Concurrency may occur because of:

- Multiple replicas
- Kubernetes deployments
- Active-active clusters
- Scheduler overlap
- Delayed execution start

The system must guarantee that at most one owner successfully acquires a job at any point in time.

---

## Decision

Lease acquisition must be implemented as a single atomic persistence operation.

Acquire operations must never rely on:

```text
Read
↓
Check
↓
Insert
```

performed as independent steps.

The persistence provider must guarantee that competing acquisitions cannot produce multiple winners.

---

## Invalid Example

The following sequence is not compliant:

```text
SELECT active execution

IF not found

INSERT active execution
```

Two competing nodes may observe the same state and both succeed.

Example:

```text
Node A -> SELECT

Node B -> SELECT

Node A -> INSERT

Node B -> INSERT
```

This may produce duplicate execution.

---

## Required Behavior

Under concurrent acquisition attempts:

```text
N contenders
```

must result in:

```text
1 winner

N - 1 losers
```

The outcome must remain deterministic regardless of timing.

---

## Lease States

An acquire attempt may observe one of three states.

### State 1 - No Active Execution

No active execution exists.

Result:

```text
Acquire succeeds
```

---

### State 2 - Active Lease Not Expired

Active execution exists.

Lease is still valid.

Result:

```text
Acquire fails
```

---

### State 3 - Active Lease Expired

Active execution exists.

Lease has expired according to authoritative database time.

Result:

```text
Acquire succeeds

ownership transfers
```

---

## Authoritative Time

Expiration decisions must use:

```text
Database UTC Time
```

See:

```text
ADR-002 Database Time Authority
```

---

## Persistence Requirements

Every persistence provider must implement atomic acquisition semantics.

Examples:

### PostgreSQL

Possible implementations:

```text
INSERT ... ON CONFLICT

UPDATE ... WHERE

transactional compare-and-swap
```

---

### SQL Server

Possible implementations:

```text
MERGE

conditional UPDATE

transactional compare-and-swap
```

---

### MySQL

Possible implementations:

```text
INSERT ... ON DUPLICATE KEY

conditional UPDATE

transactional compare-and-swap
```

---

### MongoDB

Possible implementations:

```text
findOneAndUpdate()

findAndModify()
```

with an atomic filter condition.

---

## Protocol Independence

The protocol defines required behavior.

It does not define database-specific implementation details.

Each provider may use its own mechanism as long as the observable behavior remains compliant.

---

## Contract Tests

The following contract tests validate this ADR.

```text
CT001 Acquire Free Job

CT002 Acquire Busy Job

CT004 Acquire Expired Lease

CT005 Concurrent Acquisition
```

In particular:

```text
CT005
```

must guarantee a single winner under concurrent execution.

---

## Rationale

Atomic acquisition guarantees:

- Singleton execution
- Correct behavior under concurrency
- Cross-language consistency
- Cross-database consistency
- Correct active-active behavior

Without atomic acquisition, duplicate execution becomes possible.

Duplicate execution violates the primary objective of JobGuardian.

---

## Consequences

### Positive

- Deterministic ownership
- Reduced race conditions
- Cross-provider consistency
- Correct distributed behavior

### Negative

- Provider implementations are more complex
- Database-specific acquisition logic is required

---

## Compliance

An implementation is compliant only if concurrent acquisition attempts produce a single successful owner.

Multiple successful acquisitions for the same:

```text
tenant_id
job_namespace
job_name
```

constitute a protocol violation.