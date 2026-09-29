# Quick Start

This guide shows how to create and run a job with JobGuardian in a few minutes.

---

# Prerequisites

- .NET 8
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

No additional code is required.

JobGuardian automatically starts its hosted service.

---

# Using PostgreSQL

For distributed execution across multiple application instances, enable the PostgreSQL provider.
Reference the `JobGuardian.Core` and `JobGuardian.PostgreSql` packages. Apply the repository
schema script to the target database before starting the application; the provider does not run
migrations automatically. For example, against a new database:

```sh
psql "$DATABASE_URL" -f sql/postgresql/V001_initial_schema.sql
```

The script also creates history and audit tables reserved for future work; the current runtime
uses the lease and job-state tables only. Provide `connectionString` from application
configuration rather than hard-coding credentials.

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