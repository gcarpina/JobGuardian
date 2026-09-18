# JobGuardian Contract Tests v1

Version: 1.0

Status: Draft

---

# Purpose

This document defines the mandatory contract tests that every JobGuardian SDK implementation must pass.

Examples:

- .NET SDK
- Java SDK
- Python SDK

Contract tests verify protocol compliance independently of implementation details.

---

# Contract Test Index

| ID | Name |
|----|------|
| CT001 | Acquire Free Job |
| CT002 | Acquire Busy Job |
| CT003 | Renew Lease |
| CT004 | Acquire Expired Lease |
| CT005 | Concurrent Acquisition |
| CT006 | Release Execution |
| CT007 | Failed Execution |
| CT008 | Lease Expiration Recovery |
| CT009 | Failure Policy Ignore |
| CT010 | Failure Policy RequireManualReset |
| CT011 | Manual Reset |
| CT012 | Audit Logging |
| CT013 | Tenant Isolation |
| CT014 | Correlation Tracking |
| CT015 | Database Time Authority |
| CT016 | Renew Active Lease |
| CT017 | Renew Lost Lease |

---

# CT001 - Acquire Free Job

## Scenario

No active execution exists.

## Given

No row exists in:

jobguardian_active_executions

for:

tenant_id = default
job_namespace = finance
job_name = nightly-import

## When

An owner attempts to acquire the job.

## Then

Acquire succeeds.

A row is created in:

jobguardian_active_executions

A row is created in:

jobguardian_execution_history

## Expected Result

Success

---

# CT002 - Acquire Busy Job

## Scenario

A valid lease already exists.

## Given

An active execution exists.

Lease has not expired.

## When

A second owner attempts acquisition.

## Then

Acquire fails.

No ownership change occurs.

No additional active execution is created.

## Expected Result

Failure

---

# CT003 - Renew Lease

## Scenario

Owner successfully renews an active lease.

## Given

An active execution exists.

## When

Heartbeat is executed.

## Then

renewed_at_utc is updated.

lease_until_utc is extended.

Ownership remains unchanged.

## Expected Result

Success

---

# CT004 - Acquire Expired Lease

## Scenario

Existing lease has expired.

## Given

A lease exists.

lease_until_utc is older than current database UTC time.

## When

Another owner attempts acquisition.

## Then

Acquire succeeds.

New owner becomes active owner.

A new execution history record is created.

## Expected Result

Success

---

# CT005 - Concurrent Acquisition

## Scenario

Multiple owners compete simultaneously.

## Given

No active execution exists.

## When

10 owners attempt acquisition concurrently.

## Then

Exactly one owner succeeds.

All other owners fail.

Only one active execution row exists.

## Expected Result

Single Winner

---

# CT006 - Release Execution

## Scenario

Job completes successfully.

## Given

An active execution exists.

## When

Release is called with outcome:

Success

## Then

The active execution row is removed.

The execution history row is updated.

## Expected Result

Success

---

# CT007 - Failed Execution

## Scenario

Job terminates with failure.

## Given

An active execution exists.

## When

Release is called with outcome:

Failed

## Then

The active execution row is removed.

The execution history row is updated.

Failure information is persisted.

## Expected Result

Success

---

# CT008 - Lease Expiration Recovery

## Scenario

Owner disappears unexpectedly.

## Given

An active execution exists.

Heartbeat stops.

## When

Lease expires.

## Then

The execution is classified as:

Abandoned

A new owner may acquire the same job.

## Expected Result

Success

---

# CT009 - Failure Policy Ignore

## Scenario

FailurePolicy = Ignore

## Given

A previous execution failed.

## When

The next scheduled execution starts.

## Then

Execution is allowed.

Acquire succeeds.

## Expected Result

Success

---

# CT010 - Failure Policy RequireManualReset

## Scenario

FailurePolicy = RequireManualReset

## Given

A previous execution failed.

## When

The next scheduled execution starts.

## Then

Execution is blocked.

No acquisition occurs.

The execution is marked as:

Skipped

## Expected Result

Blocked

---

# CT011 - Manual Reset

## Scenario

Operator resets a blocked job.

## Given

A job is blocked by:

RequireManualReset

## When

ResetFailureState is executed.

## Then

The job becomes eligible for execution again.

An audit record is created.

## Expected Result

Success

---

# CT012 - Audit Logging

## Scenario

Operator performs an administrative action.

## Given

An authenticated operator exists.

## When

One of the following actions is executed:

RunNow
ResetFailureState
ForceReleaseLease
ForceTakeoverLease

## Then

An audit row is written.

Required fields:

tenant_id
user_id
user_name
action
timestamp_utc

## Expected Result

Success

---

# CT013 - Tenant Isolation

## Scenario

Identical job identities exist in different tenants.

## Given

Tenant A:

tenant-a/finance/nightly-import

Tenant B:

tenant-b/finance/nightly-import

## When

Both jobs acquire execution simultaneously.

## Then

Both acquisitions succeed.

No interference occurs between tenants.

## Expected Result

Success

---

# CT014 - Correlation Tracking

## Scenario

Execution includes tracing information.

## Given

An execution starts.

## When

History is written.

## Then

The following fields are persisted:

correlation_id

conversation_id

## Expected Result

Success

---

# CT015 - Database Time Authority

## Scenario

Nodes have inconsistent local clocks.

## Given

Node A is +30 seconds.

Node B is -30 seconds.

Compared to actual time.

## When

Lease acquisition and expiration decisions are performed.

## Then

Only database UTC time is used.

Local clock differences do not affect correctness.

## Expected Result

Success

---

# Compliance

An SDK implementation is considered compliant with JobGuardian Protocol v1 only if all contract tests defined in this document pass successfully.

# CT016 - Renew Active Lease

## Scenario

Owner successfully renews a valid lease.

## Given

An active execution exists.

Lease has not expired.

## When

Heartbeat renewal is executed.

## Then

Renew succeeds.

lease_until_utc is extended.

Ownership remains unchanged.

## Expected Result

Success

# CT017 - Renew Lost Lease

## Scenario

Owner attempts to renew a lease that is no longer owned.

## Given

A lease existed.

Ownership has already been transferred.

## When

Original owner executes Renew.

## Then

Renew fails.

Ownership remains unchanged.

## Expected Result

Failure