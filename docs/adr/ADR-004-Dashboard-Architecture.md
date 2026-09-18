# ADR-004 - Dashboard Architecture

## Status

Accepted

---

## Context

JobGuardian provides:

- Singleton execution
- Execution history
- Audit trail
- Failure policies
- Operational visibility

Operators require a centralized interface to:

- View active executions
- Inspect execution history
- Review audit logs
- Trigger administrative actions

Examples:

- RunNow
- ResetFailureState
- ForceReleaseLease

A dashboard component is therefore required.

The dashboard must remain independent from job execution runtimes and SDK implementations.

---

## Decision

JobGuardian Dashboard is implemented as a standalone application.

The dashboard is not part of the execution pipeline.

The dashboard does not participate in:

- Lease acquisition
- Lease renewal
- Lease expiration
- Execution ownership

The dashboard only performs:

- Query operations
- Administrative operations
- Audit operations

---

## Architecture

```text
+---------------------------+
| Job Runtimes              |
|                           |
| .NET                      |
| Java                      |
| Python                    |
| Kubernetes                |
+-------------+-------------+
              |
              |
              v
+---------------------------+
|      jobguardian-db       |
+-------------+-------------+
              ^
              |
              |
+-------------+-------------+
|     JobGuardian Dashboard |
+---------------------------+
```

The dashboard interacts only with the shared persistence layer.

No central coordination service is required.

No execution traffic flows through the dashboard.

---

## Technology Choice

The reference implementation is:

JobGuardian.Dashboard

implemented using:

- ASP.NET Core
- Blazor Server

---

## Authentication

The dashboard must support:

- OpenID Connect (OIDC)
- OAuth2

Authentication providers are implementation-specific.

Examples:

- Keycloak
- Entra ID
- Auth0

---

## Authorization

Role-based authorization is required.

Recommended roles:

### Viewer

May:

- View active executions
- View execution history
- View audit logs

May not:

- Execute administrative actions

### Operator

May:

- View all data
- RunNow
- ResetFailureState

May not:

- Perform destructive administrative actions

### Admin

May perform all operations, including:

- ForceReleaseLease
- ForceTakeoverLease

---

## Audit Requirements

All administrative operations must generate audit entries.

Examples:

- RunNow
- ResetFailureState
- ForceReleaseLease
- ForceTakeoverLease

Audit records are stored in:

jobguardian_audit_log

---

## Multi-Tenant Requirements

The dashboard must be tenant-aware.

Examples:

- Filter by tenant
- Restrict access by tenant
- Restrict operations by tenant

Tenant visibility is determined by authorization policies.

---

## Deployment

The dashboard should support:

- Docker containers
- Kubernetes deployments
- Standalone executable deployment

The dashboard requires only:

- Database connection
- Authentication configuration

No direct dependency on job runtimes exists.

---

## Observability

The dashboard should expose:

- OpenTelemetry tracing
- OpenTelemetry metrics
- Structured logging

The same observability standards used by JobGuardian SDKs should be supported.

---

## Rationale

Separating execution from administration provides:

- Reduced coupling
- Better scalability
- Better security
- Simpler deployment
- Clear responsibility boundaries

The execution platform remains operational even if the dashboard is unavailable.

The dashboard enhances observability and governance but is not required for runtime correctness.

---

## Consequences

### Positive

- No single point of failure
- Independent deployment lifecycle
- Independent scaling
- Simpler operational model

### Negative

- Additional deployable component
- Additional authentication configuration

---

## Compliance

An implementation remains compliant with JobGuardian Protocol v1 even if the dashboard is not deployed.

The dashboard is an optional operational component and not part of the protocol core.