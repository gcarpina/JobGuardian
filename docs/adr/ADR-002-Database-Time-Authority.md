# ADR-002 - Database Time Authority

## Status

Accepted

---

## Context

JobGuardian relies on lease acquisition, lease renewal and lease expiration semantics.

Lease ownership decisions are time-sensitive.

In distributed systems, local machine clocks cannot be assumed to be perfectly synchronized.

Examples:

- Kubernetes nodes
- Virtual machines
- Containers
- Physical servers

Clock drift may result in inconsistent lease decisions.

Examples:

- Early lease expiration
- Late lease expiration
- Duplicate execution
- Missed execution opportunities

The problem becomes more significant in active-active deployments spanning multiple clusters, regions or datacenters.

---

## Decision

Database UTC time is the authoritative time source for all lease-related decisions.

The following operations must use database UTC time:

- Lease acquisition
- Lease renewal
- Lease expiration validation
- Execution timestamps
- Audit timestamps

Implementations must not use local machine time to determine lease ownership or expiration.

Examples of prohibited approaches:

- DateTime.UtcNow
- System.currentTimeMillis()
- datetime.utcnow()

when used as the authoritative source for lease decisions.

Local clocks may be used only for:

- Logging
- Diagnostics
- Performance measurements
- Client-side display

---

## Rationale

Using database UTC time guarantees that all competing execution nodes share the same authoritative time source.

Advantages:

- Consistent lease expiration
- Consistent lease renewal
- Reduced clock drift issues
- Deterministic conflict resolution
- Cross-language consistency

This approach simplifies protocol compliance for:

- .NET SDK
- Java SDK
- Python SDK

because all implementations follow the same temporal reference.

---

## Consequences

### Positive

- Deterministic lease ownership
- Reduced risk of duplicate executions
- Simpler protocol definition
- Easier contract testing
- Better multi-cluster support

### Negative

- Additional database interaction may be required
- Lease operations depend on database availability
- Database time becomes a critical dependency

---

## Implementation Notes

Examples of acceptable database time retrieval:

### PostgreSQL

Current UTC timestamp obtained from the database.

### SQL Server

Current UTC timestamp obtained from the database.

### MySQL

Current UTC timestamp obtained from the database.

### MongoDB

Server-side timestamp mechanisms should be used whenever possible.

Implementations may cache database time briefly for performance reasons, but lease decisions must remain consistent with the authoritative database clock.

---

## Compliance

An implementation that uses local machine time as the authoritative source for lease ownership decisions is not compliant with JobGuardian Protocol v1.