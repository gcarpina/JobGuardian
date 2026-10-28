# Quick Start

This guide shows how to create and run a job with JobGuardian in a few minutes.

---

# Prerequisites

- .NET 10 (recommended); .NET 8 remains a target while technically compatible
- Dependency Injection
- Hosted Services

PostgreSQL is optional for local, single-process scenarios. It is required when multiple
application instances must coordinate execution.

The in-memory configuration does not coordinate leases across processes and loses its lease state
when the process exits.

---

# Step 1 - Create a Job

```csharp
using JobGuardian.Abstractions.Contracts;

public sealed class InvoiceSynchronizationJob
    : IJob
{
    public Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        Console.WriteLine(
            "Synchronizing invoices...");

        return Task.CompletedTask;
    }
}
```

---

# Step 2 - Register JobGuardian

## In-Memory Configuration

```csharp
using JobGuardian.Core.DependencyInjection;

services.AddJobGuardian();
```

This configuration requires no external infrastructure and is suitable for local development or a
single application process. It does not provide distributed coordination across multiple instances.

Job leases and job state are stored in memory using:

```text
InMemoryLeaseStore
InMemoryJobStateRepository
```

---

# Step 3 - Register a Job

```csharp
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.DependencyInjection;
using JobGuardian.Core.Models;

services.AddJob<InvoiceSynchronizationJob>(
    new JobKey(
        "tenant-a",
        "billing",
        "invoice-sync"),
    new JobExecutionPolicy
    {
        LeaseDuration = TimeSpan.FromMinutes(1),
        HeartbeatInterval = TimeSpan.FromSeconds(10),
        FailurePolicy = FailurePolicy.RequireManualReset,
        MaxAttempts = 3,
        RetryDelay = TimeSpan.FromSeconds(5)
    });
```

`MaxAttempts` counts total callback invocations, including the initial one. It defaults to `1`
(no retry), accepts values from 1 to 10, and uses a fixed `RetryDelay` between failed attempts
(default one second; maximum five minutes). A zero delay is allowed. Only callback failures are
retried; skipped executions, cancellation, and lease loss are terminal outcomes. The same lease and
heartbeat cover all callback attempts and the retry delays.

The retry delay is fixed; exponential backoff and jitter are not currently provided. `MaxAttempts`
limits callback invocations, not elapsed time, because JobGuardian does not impose a callback
timeout. A slow or stuck callback can continue holding the lease while its heartbeat runs.

An exception does not prove that a callback's side effects did not happen. The callback may be
invoked again after partially or fully completing an external operation. Make retryable work
idempotent, use a stable business idempotency key, or deduplicate side effects in the system that
owns them. JobGuardian does not provide exactly-once execution. The execution history and failure
policy record or act on the final outcome of the coordinated execution, not each internal callback
attempt.

---

# Step 4 - Run the Application

When the application starts:

```text
Acquire Lease
        ↓
Execute Job
        ↓
Maintain Heartbeat
        ↓
Release Lease
```

JobGuardian automatically starts its hosted service, which checks registered jobs at the
configured polling interval (30 seconds by default).

---

# Using PostgreSQL

For distributed execution across multiple application instances, enable the PostgreSQL provider.
Reference the `JobGuardian.Core` and `JobGuardian.PostgreSql` packages. Apply the repository
schema script to the target database before starting the application; the provider does not run
migrations automatically. The script is available in the repository at
`sql/postgresql/` and all versioned scripts are included in the `JobGuardian.PostgreSql` package.
NuGet stores it in the package cache; it does not copy the script into the application project or
apply it automatically. See the [package consumer sample](../dotnet/samples/PackageConsumer/README.md)
for the cache path and a `psql` example. Against a database and from the repository root, the
repository scripts must be applied in order:

```sh
psql "$DATABASE_URL" -f sql/postgresql/V001_initial_schema.sql
```

`V001_initial_schema.sql` creates the complete schema for a new database in one transaction. It is
not a rerunnable upgrade script and must not be applied to a database already initialized by an
earlier preview. The current preview does not provide an upgrade path for those databases.

The PostgreSQL provider also persists execution attempts in
`jobguardian_execution_history`, including correlation ID, lease-acquisition time, terminal
`ExecutionOutcome`, outcome reason, and failure category. History writes are best-effort and do not
replace the callback outcome. A paginated query API is available; history is not deleted
automatically, so operators must define and execute an appropriate retention policy.

Programmatic resets require the authenticated actor's stable ID from the hosting application. The
SDK writes a `ResetJobRequested` audit record before changing state, including idempotent resets.
If the audit write fails, the reset is not applied; if the subsequent state write fails, the audit
record remains as evidence of the request, not a claim that the reset completed. PostgreSQL audit
records are durable and retention is operator-owned. InMemory audit records are process-local and
lost on restart. JobGuardian does not authenticate or authorize the actor.

Provide `connectionString` from application configuration rather than hard-coding credentials.

By default, JobGuardian generates a correlation ID for each coordinated execution and retains it
across callback retries. A host can supply its own value by registering an
`IExecutionCorrelationIdProvider`. OpenTelemetry trace context is separate and follows the current
`Activity`.

```csharp
using JobGuardian.Core.DependencyInjection;
using JobGuardian.PostgreSql.DependencyInjection;

services
    .AddJobGuardian()
    .UsePostgreSql(
        connectionString);
```

---

# OpenTelemetry: metrics and traces

`JobGuardian.Core` publishes execution and lease metrics and spans through the standard .NET
`Meter` and `ActivitySource` APIs. The library does not include an OpenTelemetry SDK, exporter,
collector, or backend: the application chooses and configures these.

Install the hosting and OTLP exporter packages in the application that hosts JobGuardian:

```sh
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

Register the JobGuardian meter and activity source with the application’s telemetry pipeline:

```csharp
using JobGuardian.Core.Observability;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService("billing-worker"))
    .WithMetrics(metrics =>
        metrics.AddMeter(
            JobGuardianInstrumentation.MeterName)
            .AddOtlpExporter())
    .WithTracing(tracing =>
        tracing.AddSource(
            JobGuardianInstrumentation.ActivitySourceName)
            .AddOtlpExporter());
```

Point the application at an OTLP receiver, such as an OpenTelemetry Collector. For example:

```sh
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

The receiver address, protocol, authentication, and TLS settings depend on the deployment. Start
the collector/backend separately; registering the exporter does not create one. Without an
OpenTelemetry listener/exporter, the application will not collect or expose JobGuardian telemetry.

The current metric instruments are:

| Metric | Meaning | Attributes |
|---|---|---|
| `jobguardian.execution.started` | Coordination attempts started, including attempts later skipped | None |
| `jobguardian.execution.attempts` | Completed attempts by outcome | `outcome` |
| `jobguardian.execution.retries` | Callback retries scheduled after a failed attempt | None |
| `jobguardian.execution.duration` | Attempt duration in seconds | `outcome` |
| `jobguardian.execution.active` | Attempts currently being coordinated | None |
| `jobguardian.lease.active` | Leases currently held by this process | None |
| `jobguardian.lease.operations` | Acquire, renew, and release operation counts | `operation`, `result` |
| `jobguardian.lease.operation.duration` | Lease-operation duration in seconds | `operation`, `result` |

Traces contain an `ExecutionAttempt` parent with child spans for lease acquisition, job execution,
heartbeat renewals, and lease release. They include job namespace/name and execution ID attributes.
Each callback span includes `jobguardian.execution.callback.attempt`; the parent includes the
configured `jobguardian.execution.max_attempts`.
Metrics deliberately omit job, tenant, owner, and execution identifiers to prevent high-cardinality
series. The active-lease gauge is process-local; aggregate it across the worker instances to see
the deployment total. Exception messages are not added to telemetry by default.

This initial runtime does not yet expose blocked-job counts, abandoned-execution counts, or
administrative-action metrics. Those signals require state enumeration or administrative APIs that
are not currently part of the .NET runtime.

For production, configure service identity, endpoint security, sampling, retention, access control,
and alerting in the application/collector environment. Do not put tenant or execution identifiers
into metric labels; use traces for sampled diagnosis and execution history for persisted outcomes.

JobGuardian uses the host's standard `ILogger` pipeline for runtime logs. Successful and skipped
attempts are logged at `Debug`; failures at `Error`, lease loss at `Warning`, and cooperative
cancellation at `Information`. Successful heartbeat renewals are logged at `Trace` only, so they
can be enabled temporarily for diagnostics without adding routine log volume. Configure providers
and levels in the hosting application; job keys and execution IDs appear in execution logs for
correlation. Each scheduled retry emits a `Warning` with its next attempt number and configured
delay. Terminal outcome logs include `DurationMilliseconds` for the coordinator attempt,
covering lease acquisition, job execution, heartbeat monitoring, and lease release; skipped
attempts report the duration of their lease-acquisition attempt. Apply the application's normal
log access and retention controls.

---

# Failure Policies

## Ignore

```csharp
FailurePolicy.Ignore
```

Execution failures do not block future executions.

```text
Failed
   ↓
Eligible
```

---

## RequireManualReset

```csharp
FailurePolicy.RequireManualReset
```

Execution failures block future executions.

```text
Failed
   ↓
Blocked
```

A manual reset is required before execution can continue.

---

# Reset a Blocked Job

Resolve `IJobStateManager` from the application's dependency injection container:

```csharp
using JobGuardian.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

var jobStateManager =
    serviceProvider.GetRequiredService<IJobStateManager>();

await jobStateManager.ResetAsync(
    jobKey,
    actorId);
```

`actorId` must be a non-empty stable identifier from the host's authenticated principal. Do not
accept a caller-supplied identity without host authorization.

State transition:

```text
Blocked
    ↓
Reset
    ↓
Eligible
```

---

# Next Steps

For implementation details see:

```text
docs/architecture/Architecture-Overview.md
```

For design decisions see:

```text
docs/architecture/ADR-*
```