# Quick Start

This guide shows how to create and run a job with JobGuardian in a few minutes.

---

# Prerequisites

- .NET 8 or .NET 10
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
        FailurePolicy = FailurePolicy.RequireManualReset
    });
```

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
`sql/postgresql/V001_initial_schema.sql` and is included in the `JobGuardian.PostgreSql` package.
NuGet stores it in the package cache; it does not copy the script into the application project or
apply it automatically. See the [package consumer sample](../dotnet/samples/PackageConsumer/README.md)
for the cache path and a `psql` example. Against a database and from the repository root, the
repository copy can be applied with:

```sh
psql "$DATABASE_URL" -f sql/postgresql/V001_initial_schema.sql
```

The PostgreSQL provider also persists execution attempts in
`jobguardian_execution_history`. Records include the attempt timestamps, `ExecutionOutcome`, and
a failure category; exception messages are not stored. History writes are best-effort and do not
replace the execution outcome. The initial implementation has no history query API or automatic
retention, so define a retention policy for long-running deployments. The audit table remains
reserved for future work. Provide `connectionString` from application configuration rather than
hard-coding credentials.

```csharp
using JobGuardian.Core.DependencyInjection;
using JobGuardian.PostgreSql.DependencyInjection;

services
    .AddJobGuardian()
    .UsePostgreSql(
        connectionString);
```

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
    jobKey);
```

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