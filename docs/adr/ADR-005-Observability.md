# ADR-005 - Observability

## Status

Accepted

---

## Context

JobGuardian coordinates execution across:

- Processes
- Services
- Containers
- Kubernetes clusters

Operational visibility is required to diagnose:

- Lease acquisition failures
- Lease expiration
- Duplicate execution prevention
- Job failures
- Recovery behavior
- Administrative actions

Without a standardized observability model, troubleshooting distributed execution becomes difficult and inconsistent across SDK implementations.

---

## Decision

JobGuardian adopts OpenTelemetry as the standard observability model.

All SDK implementations should support:

- Metrics
- Traces
- Structured Logs

The reference monitoring stack is:

- OpenTelemetry
- Prometheus
- Grafana
- Jaeger

---

## Metrics

The following metrics are part of JobGuardian Protocol v1.

### Lease Metrics

```text
jobguardian_lease_acquired_total
jobguardian_lease_acquire_failed_total
jobguardian_lease_renew_total
jobguardian_lease_lost_total
```

### Execution Metrics

```text
jobguardian_execution_started_total
jobguardian_execution_completed_total
jobguardian_execution_failed_total
jobguardian_execution_abandoned_total
```

### Duration Metrics

```text
jobguardian_execution_duration_seconds
```

Type:

```text
Histogram
```

### State Metrics

```text
jobguardian_active_leases
jobguardian_blocked_jobs
```

Type:

```text
Gauge
```

### Administrative Metrics

```text
jobguardian_manual_runs_total
jobguardian_force_release_total
```

---

## Labels

Metrics should expose the following labels whenever applicable:

```text
tenant_id
job_namespace
job_name
environment
application_name
run_type
outcome
```

Labels should remain stable across SDK implementations.

---

## Tracing

OpenTelemetry tracing is strongly recommended.

The following operations should create spans:

```text
AcquireLease
RenewLease
ReleaseLease
ExecuteJob
RunNow
ResetFailureState
ForceReleaseLease
```

---

## Correlation

Tracing should support:

```text
correlation_id
conversation_id
```

These identifiers should be propagated whenever possible.

Execution history records should persist both values when they are available to the runtime.
The initial .NET hosted runtime leaves them unset until a correlation propagation contract is
introduced.

---

## Structured Logging

Structured logging is recommended.

Logs should include:

```text
tenant_id
job_namespace
job_name
execution_id
owner_id
correlation_id
conversation_id
```

Implementations should avoid unstructured log messages when structured alternatives are available.

---

## Dashboard Integration

JobGuardian.Dashboard should be able to:

- Consume metrics
- Display execution statistics
- Correlate execution history with traces
- Navigate from executions to traces

Dashboard functionality must not depend on observability tooling availability.

---

## Prometheus

Metrics must be exposed in a format compatible with Prometheus scraping.

Direct Prometheus integration and OpenTelemetry-based integration are both acceptable.

---

## Jaeger

Jaeger is the reference tracing backend for the initial implementation.

Future tracing backends may include:

- Grafana Tempo
- Azure Monitor
- Elastic APM
- Other OpenTelemetry-compatible systems

No vendor-specific dependency should be introduced into the protocol.

---

## Multi-Language Requirements

Observability behavior should be consistent across:

- .NET SDK
- Java SDK
- Python SDK

Metric names, labels and tracing semantics must remain equivalent.

---

## Rationale

A shared observability model provides:

- Easier troubleshooting
- Consistent monitoring
- Cross-language visibility
- Better operational governance

Using OpenTelemetry avoids vendor lock-in while remaining compatible with widely adopted observability platforms.

---

## Consequences

### Positive

- Standardized monitoring
- Standardized tracing
- Cross-language consistency
- Future-proof observability model

### Negative

- Additional implementation effort
- Additional runtime telemetry overhead

---

## Compliance

An SDK implementation is compliant with JobGuardian Protocol v1 only if it exposes the required protocol-defined metrics.

Tracing support is strongly recommended and should be considered mandatory for production deployments.