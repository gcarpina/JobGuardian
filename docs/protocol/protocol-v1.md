# JobGuardian Protocol v1

Version: 1.0

Status: Draft

---

# 1. Vision

JobGuardian enables singleton execution of jobs across multiple nodes, processes, containers and Kubernetes clusters using a shared persistence store.

JobGuardian is not a scheduler.

Scheduling is delegated to external schedulers such as:

- Kubernetes CronJobs
- Linux Cron
- Windows Task Scheduler
- BackgroundService loops
- Custom schedulers

---

# 2. Core Concepts

## Tenant

A logical isolation boundary.

## Job

A uniquely identifiable execution target.

A job is defined by:

- tenant_id
- job_namespace
- job_name

## Execution

A single execution instance of a Job.

## Owner

The runtime instance currently owning an active execution.

Recommended format:

environment/application/instance

Example:

production/invoice-service/pod-123

## Lease

A distributed coordination mechanism used to ensure singleton execution.

---

# 3. Job Identity

The unique logical identity of a Job is:

tenant_id + job_namespace + job_name

Example:

default/finance/nightly-import

---

# 4. Acquire Semantics

An execution may acquire a lease if:

- no active execution exists

OR

- existing lease is expired

An execution must not acquire a lease if:

- a valid lease already exists

The authoritative time source is the database UTC time.

Local clocks must never be used for lease ownership decisions.

---

# 5. Heartbeat Semantics

Each active execution owns:

- lease_duration
- heartbeat_interval

Constraint:

heartbeat_interval < lease_duration

Recommended:

heartbeat_interval <= lease_duration / 3

Heartbeat renews ownership of the lease.

---

# 6. Release Semantics

When execution completes:

- update execution history
- remove active execution

Release must occur for:

- Success
- Failed

Release is not required for crash recovery.

---

# 7. Expiration Semantics

If an owner disappears:

- crash
- process termination
- node failure
- pod deletion
- network isolation

heartbeat stops.

The lease eventually expires.

A new owner may acquire the execution.

---

# 8. Failure Policies

## Ignore

Execution may run regardless of previous execution outcome.

## RequireManualReset

Execution is blocked after failure until manually reset.

---

# 9. Outcomes

Supported outcomes:

- Success
- Failed
- Abandoned
- Skipped

---

# 10. Run Types

Supported run types:

- Scheduled
- Manual
- Recovery

---

# 11. Triggered By

Supported trigger sources:

- Scheduler
- Operator
- RecoveryPolicy
- Api

---

# 12. Tracing

Each execution may define:

- correlation_id
- conversation_id

---

# 13. Versioning

Each execution must contain:

- protocol_version
- schema_version

---

# 14. Multi Tenant Model

tenant_id is mandatory.

Implementations must support:

default

as the default tenant identifier.

---

# 15. Retention

Execution history retention is configurable.

Default retention:

365 days

---

# 16. Observability

Implementations should expose:

- metrics
- traces
- logs

OpenTelemetry is the recommended observability model.

Prometheus-compatible metrics are required.

---

# 17. Security

Dashboards and administrative tools should support:

- OIDC
- OAuth2
- Role Based Access Control

Recommended roles:

- Viewer
- Operator
- Admin

# 18. Acquire Algorithm

Acquire operations must be atomic.

Inputs:

- tenant_id
- job_namespace
- job_name
- execution_id
- owner_id
- lease_duration

Algorithm:

Acquire(job)

    dbNow = GetDatabaseUtcTime()

    leaseUntilUtc = dbNow + LeaseDuration

    Attempt Atomic Acquire

        IF no active execution exists

            create active execution

            return Success

        ELSE IF lease is expired

            transfer ownership

            return Success

        ELSE

            return Failure

The implementation of atomic acquisition is provider-specific.

Observable behavior must remain consistent across all providers.

# 19. Renew Algorithm

Renew(execution)

    dbNow = GetDatabaseUtcTime()

    newLeaseUntilUtc =
        dbNow + LeaseDuration

    Success =
        Atomic Renew

        WHERE

            Job matches

            AND

            ExecutionId matches

            AND

            Lease is not expired

        SET

            RenewedAtUtc = dbNow

            LeaseUntilUtc = newLeaseUntilUtc

    IF Success

        return Success

    ELSE

        return Failure

# 20. Release Algorithm

Release(execution)

    Update execution history

        Outcome
        FailureCategory
        EndedAtUtc

    Remove active execution

    Return Success

# 21. Expiration Algorithm

ExpirationCheck(activeExecution)

    dbNow = GetDatabaseUtcTime()

    IF

        LeaseUntilUtc < dbNow

    THEN

        Lease is expired

        Ownership is lost

        Another owner may acquire

# 22. Ownership Rules

For a given:

tenant_id
job_namespace
job_name

there can be at most one active owner.

Ownership is transferred only when:

- lease expires

or

- current owner releases execution

Ownership decisions are based exclusively on:

Database UTC Time
