# ADR-009 - Ownership Rules

## Status

Accepted

---

## Decision

For a given:

tenant_id
job_namespace
job_name

there may be at most one active owner.

Ownership may change only when:

- the current owner releases execution
- the current lease expires

Ownership decisions must use database UTC time as the authoritative source.

---

## Consequences

- Singleton execution is guaranteed
- Lease ownership is deterministic
- Concurrent acquisition remains safe

---

## Compliance

Multiple active owners for the same job identity constitute a protocol violation.