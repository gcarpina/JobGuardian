# ADR-003 - Multi Tenant Model

## Status

Accepted

---

## Context

JobGuardian is designed to support:

- Multiple applications
- Multiple teams
- Multiple environments
- Multiple clusters
- Multiple organizations

A future deployment may require logical isolation between independent tenants while sharing the same physical persistence store.

Examples:

- Different customers
- Different business units
- Different departments
- Independent operational domains

Adding multi-tenancy after large volumes of execution history have been collected would introduce significant migration complexity.

---

## Decision

JobGuardian adopts a tenant-aware data model from Protocol v1.

Every job execution belongs to a tenant.

The tenant identifier is represented by:

tenant_id

The unique logical identity of a Job is:

tenant_id + job_namespace + job_name

Examples:

default/finance/nightly-import

customer-a/finance/nightly-import

customer-b/finance/nightly-import

Independent tenants may use identical namespaces and job names without conflicts.

---

## Default Tenant

To simplify adoption for single-tenant deployments, JobGuardian defines a default tenant.

The reserved default tenant identifier is:

default

Applications that do not require tenant isolation may use:

tenant_id = default

This allows single-tenant installations to adopt the protocol without additional complexity.

---

## Database Model

The tenant identifier must be present in all protocol-defined tables.

Examples:

- jobguardian_active_executions
- jobguardian_execution_history
- jobguardian_audit_log

The tenant identifier participates in the logical identity of a job.

Example unique key:

tenant_id
job_namespace
job_name

---

## Dashboard Model

Dashboards and administrative tools should be tenant-aware.

Examples:

- Filter by tenant
- Restrict visibility by tenant
- Restrict operations by tenant

Tenant isolation should be enforced by application authorization policies.

---

## Security Model

Authentication is independent from tenancy.

Authorization may restrict users to one or more tenants.

Examples:

Operator A

allowed tenants:

- finance

Operator B

allowed tenants:

- customer-a
- customer-b

The mechanism used to provide tenant information is implementation-specific.

Examples:

- Keycloak groups
- Keycloak roles
- OIDC claims
- Custom authorization providers

---

## Rationale

Introducing multi-tenancy from the first protocol version provides:

- Forward compatibility
- Simpler future evolution
- Reduced migration complexity
- Better SaaS readiness
- Improved isolation

The default tenant keeps the onboarding experience simple for single-tenant deployments.

---

## Consequences

### Positive

- Future-proof data model
- Tenant isolation support
- Shared infrastructure support
- Better authorization capabilities

### Negative

- Slightly larger primary keys
- Additional filtering requirements
- Increased implementation complexity

---

## Implementation Notes

Tenant isolation is a protocol concern.

Implementations must ensure that operations affecting a tenant cannot accidentally impact another tenant.

Contract tests must verify tenant isolation behavior.

See:

CT013 - Tenant Isolation

---

## Compliance

An implementation is compliant only if jobs with identical:

- job_namespace
- job_name

can execute independently when they belong to different tenants.