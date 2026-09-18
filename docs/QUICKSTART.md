# Quick Start

This guide shows how to create and run a distributed job with JobGuardian in a few minutes.

---

# Prerequisites

- .NET 8
- Dependency Injection
- Hosted Services

PostgreSQL is optional.

The framework can run entirely in memory.

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
services.AddJobGuardian();
```

This configuration requires no external infrastructure.

Job state is stored using:

```text
InMemoryJobStateRepository
```

---

# Step 3 - Register a Job

```csharp
services.AddJob<InvoiceSynchronizationJob>(
    new JobKey(
        "tenant-a",
        "billing",
        "invoice-sync"),
    JobExecutionPolicy.IgnoreFailures);
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

```csharp
services
    .AddJobGuardian()
    .UsePostgreSql(
        connectionString);
```

---

# Failure Policies

## IgnoreFailures

```csharp
JobExecutionPolicy.IgnoreFailures
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
JobExecutionPolicy.RequireManualReset
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

```csharp
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