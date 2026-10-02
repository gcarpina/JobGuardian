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

## .NET Instrumentation

`JobGuardian.Core` publishes instrumentation using the .NET `Meter` and `ActivitySource` APIs.
It does not depend on an OpenTelemetry SDK or exporter. Applications opt in by configuring
OpenTelemetry to listen to the names exported by `JobGuardianInstrumentation`:

```text
Meter:          JobGuardian
ActivitySource: JobGuardian
```

The original Protocol v1 metric requirements remain the target contract:

| Required signal | Current .NET coverage |
|---|---|
| `jobguardian_lease_acquired_total` | `jobguardian.lease.operations` with `operation=acquire`, `result=acquired` |
| `jobguardian_lease_acquire_failed_total` | `jobguardian.lease.operations` with `operation=acquire`, `result=not_acquired` or `result=error` |
| `jobguardian_lease_renew_total` | `jobguardian.lease.operations` with `operation=renew` |
| `jobguardian_lease_lost_total` | Only an approximate signal exists: lease operations with `result=not_owner`; no one-to-one loss counter |
| `jobguardian_execution_started_total` | `jobguardian.execution.started` counts coordination attempts, including attempts later skipped |
| `jobguardian_execution_completed_total` | `jobguardian.execution.attempts` counts terminal coordination outcomes, including skipped attempts |
| `jobguardian_execution_failed_total` | Derivable from `jobguardian.execution.attempts` with `outcome=Failed`; the instrument name differs |
| `jobguardian_execution_abandoned_total` | Not emitted; abrupt process termination cannot reliably emit a completion counter |
| `jobguardian_execution_duration_seconds` | `jobguardian.execution.duration` |
| `jobguardian_active_leases` | `jobguardian.lease.active`, a process-local observable gauge |
| `jobguardian_blocked_jobs` | Not emitted; current state APIs do not enumerate the blocked-job population |
| `jobguardian_manual_runs_total` | Not emitted; no administrative/manual-run API is wired to runtime telemetry |
| `jobguardian_force_release_total` | Not emitted; no force-release API exists in the .NET runtime |

Coverage in this table is semantic, not an exact OpenTelemetry instrument-name or Prometheus
exported-name mapping. The current names do not directly reproduce every protocol metric name, and
the start/completion counters measure coordination attempts rather than only callbacks that ran
under a lease. In particular, abandoned executions, exact lease-loss counting, blocked-job
population and administrative-action metrics remain gaps; do not treat the current implementation
as fully compliant with the original Protocol v1 metric contract.

The current .NET metric instruments are:

| Instrument | Type | Unit | Attributes |
|---|---|---|---|
| `jobguardian.execution.started` | Counter | `{attempt}` | None |
| `jobguardian.execution.attempts` | Counter | `{attempt}` | `outcome` |
| `jobguardian.execution.retries` | Counter | `{retry}` | None |
| `jobguardian.execution.duration` | Histogram | `s` | `outcome` |
| `jobguardian.execution.active` | UpDownCounter | `{execution}` | None |
| `jobguardian.lease.active` | ObservableGauge | `{lease}` | None |
| `jobguardian.lease.operations` | Counter | `{operation}` | `operation`, `result` |
| `jobguardian.lease.operation.duration` | Histogram | `s` | `operation`, `result` |

`jobguardian.execution.duration` measures the full coordination attempt, including lease
acquisition and release, rather than callback time alone. `outcome` uses `Succeeded`, `Failed`,
`Cancelled`, `LeaseLost`, or `Skipped`. `Error` is a
telemetry-only value used if coordination throws before it can return an `ExecutionResult`; it is
not an `ExecutionOutcome` and is never persisted to history.
`operation` uses `acquire`, `renew`, or `release`. `result` uses `acquired`, `not_acquired`,
`renewed`, `released`, `not_owner`, `cancelled`, or `error`, as applicable.

Metric attributes are intentionally low-cardinality. Job keys, tenant IDs, execution IDs,
owner IDs, and exception messages are not metric attributes.

The current .NET trace hierarchy includes:

```text
JobGuardian.ExecutionAttempt
├── JobGuardian.AcquireLease
├── JobGuardian.ExecuteJob
│   └── JobGuardian.RenewLease (one span per renewal attempt)
└── JobGuardian.ReleaseLease
```

Spans include `jobguardian.job.namespace`, `jobguardian.job.name`, and
`jobguardian.execution.id`. Lease spans additionally include `jobguardian.lease.operation` and
`jobguardian.lease.result`. Failed and lease-lost outcomes are marked as errors; expected skipped
attempts and cooperative cancellations are not. Exception messages and owner/tenant identifiers
are not added by default. Each `JobGuardian.ExecuteJob` span includes
`jobguardian.execution.callback.attempt`, and the parent execution span includes
`jobguardian.execution.max_attempts`. The retry counter counts callback retries scheduled after a
failure; it does not count the initial invocation or retries prevented by cancellation or lease
loss.

The original trace requirements also call for spans around `RunNow`, `ResetFailureState`, and
`ForceReleaseLease`. These administrative APIs do not exist in the current .NET runtime and
therefore do not currently emit spans.

The original label guidance is refined for OpenTelemetry: `environment` and `application_name`
belong in the service resource, while `outcome` is a bounded metric attribute. Tenant and job
identifiers are deliberately omitted from metric attributes by default because they can create
high-cardinality series and expose sensitive context. `run_type` is not currently available on
`ActiveExecution`; adding it requires a runtime contract change. Job identity remains available on
sampled traces and persisted execution history.

The .NET implementation does not yet emit blocked-job population or administrative-action
metrics/spans, or correlation/conversation identifiers. Abandoned executions cannot be counted
reliably after abrupt process termination; operational consumers should combine persisted history,
lease state and process health rather than interpret a missing completion event as a reliable
counter.

Metric names, labels, and trace semantics must remain equivalent across SDKs as required by
Protocol v1. The current .NET names are its implementation mapping; Java and Python parity remains
unimplemented and must be verified before claiming cross-language compliance.

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

## Export and Collection

The application owns OpenTelemetry SDK registration, resource identity, exporters, endpoint
configuration, sampling, and collector/backend operations. JobGuardian does not expose an HTTP
metrics endpoint or send telemetry until an application configures a listener and exporter. The
configured exporter must expose a Prometheus-compatible scrape or forward metrics through a
Prometheus-compatible collector when Prometheus is the monitoring backend.

For Prometheus deployments, applications may use an OpenTelemetry Prometheus exporter or export
through an OpenTelemetry Collector. Avoid putting tenant, job, or execution identifiers on metric
labels; use sampled traces or the execution history for per-execution diagnosis.

---

## Tracing Backends

No tracing backend is required by the library. OTLP-compatible backends such as Jaeger, Grafana
Tempo, and cloud observability services can be selected by the application.

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

This ADR defines the target observability direction. The initial .NET implementation emits the
metrics and spans listed above. Structured log export, administrative instrumentation, state
gauges, sampling policy, and equivalent Java/Python implementations remain follow-up work.

Production applications must configure an exporter and collector/backend, choose an appropriate
sampling and retention policy, protect access to telemetry data, and alert on service objectives
that matter to their workload.