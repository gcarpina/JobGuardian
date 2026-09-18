# ADR-013: Persistent Job State Storage

## Status

Accepted

---

## Context

ADR-012 introduced the Job State Model:

```text
Eligible
Running
Blocked
```

Subsequent iterations introduced:

```text
JobStateTransitionEngine

IJobStateRepository

JobStateManager

Runtime State Enforcement
```

A blocked job must remain blocked across:

- application restarts
- service redeployments
- node failures
- cluster failovers

A persistent storage strategy is therefore required.

---

## Problem

The runtime requires durable storage for non-default job states.

The design must remain:

- simple
- efficient
- scalable
- operationally sustainable

The solution should avoid storing unnecessary state.

---

## Decision

Persist only non-default states.

Current implementation:

```text
Persisted:
    Blocked

Not Persisted:
    Eligible
```

The runtime adopts the following rule:

```text
Missing Record
        ↓
Eligible
```

```text
Persisted Record
        ↓
Blocked
```

---

## Rationale

Eligible represents the default operational state.

Persisting default state would provide no additional information.

Persisting only non-default states provides:

- smaller tables
- fewer writes
- fewer updates
- simpler migrations
- simpler operational model

The repository stores only information that changes runtime behavior.

---

## Repository Semantics

### GetAsync

```text
Missing Record
        ↓
null
```

The runtime resolves:

```text
null
        ↓
Eligible
```

via JobStateManager.

---

### SetAsync(Blocked)

Persist state.

Example:

```text
Blocked
        ↓
UPSERT
```

---

### SetAsync(Eligible)

Remove persisted state.

Example:

```text
Blocked
        ↓
Eligible
        ↓
DELETE RECORD
```

---

## Storage Model

Suggested schema:

```sql
CREATE TABLE job_state
(
    tenant          VARCHAR(100) NOT NULL,
    domain          VARCHAR(100) NOT NULL,
    job_name        VARCHAR(200) NOT NULL,

    state           VARCHAR(50) NOT NULL,

    updated_at_utc  TIMESTAMPTZ NOT NULL,

    PRIMARY KEY
    (
        tenant,
        domain,
        job_name
    )
);
```

---

## Consequences

### Positive

- minimal storage footprint
- reduced write volume
- simpler operational model
- scalable for large installations
- aligns with runtime semantics

### Negative

- runtime must preserve the rule:

```text
Missing Record
        ↓
Eligible
```

- dashboards must account for implicit Eligible state

---

## Alternatives Considered

### Persist All States

```text
Eligible
Blocked
```

Rejected.

Reason:

```text
Eligible
```

is the default state and does not carry meaningful information.

Persisting it would increase storage and operational complexity without providing additional value.

---

## Design Principle

Persist only information that changes runtime behavior.

Current behavior-changing state:

```text
Blocked
```

Default state:

```text
Eligible
```

must remain implicit.