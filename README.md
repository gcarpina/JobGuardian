# JobGuardian

A cloud-native friendly framework for distributed job coordination.

JobGuardian provides lease-based execution ownership, failure policy enforcement, runtime state management, and optional persistence providers, allowing multiple application instances to safely coordinate recurring workloads without duplicate execution.

---

# What Is JobGuardian?

JobGuardian is a distributed job coordination framework.

Its responsibility is to determine:

- who may execute a workload
- who currently owns execution
- when ownership is lost
- how failures affect future executions
- when execution may resume

JobGuardian is intentionally focused on execution coordination rather than scheduling.

Scheduling remains the responsibility of the hosting application or an external scheduler.

```text
Scheduling
    ↓
Cron
Quartz
Hangfire

Execution Coordination
    ↓
JobGuardian
```

---

# Why JobGuardian?

Running recurring workloads in distributed environments introduces challenges such as:

- duplicate execution
- race conditions
- ownership conflicts
- inconsistent processing
- failure recovery

Without coordination, multiple application instances may attempt to execute the same logical workload simultaneously.

JobGuardian guarantees that only one execution owner may execute a workload at any given point in time.

---

# Key Features

- Distributed lease-based execution ownership
- Single-owner execution guarantees
- Heartbeat-based ownership renewal
- Failure policy enforcement
- Runtime state management
- Manual reset workflows
- In-memory provider for local and single-process scenarios
- PostgreSQL provider
- Fluent registration model
- Provider-based architecture

---

# Cloud-Native Friendly

JobGuardian is designed to be cloud-native friendly.

Key characteristics include:

- stateless runtime components
- externalized persistence
- distributed coordination through leases
- support for multi-instance deployments
- provider-based infrastructure abstraction
- container-friendly execution model

The in-memory provider coordinates executions only within one process. Use the PostgreSQL provider
when multiple application instances must coordinate through shared leases.

JobGuardian does not depend on Kubernetes-specific APIs and can be deployed in any environment capable of hosting the current implementation.

---

# Quick Example

## Configure JobGuardian

In-memory configuration:

```csharp
services.AddJobGuardian();
```

PostgreSQL configuration:

```csharp
services
    .AddJobGuardian()
    .UsePostgreSql(
        connectionString);
```

---

## Define a Job

```csharp
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

## Register a Job

```csharp
services.AddJob<InvoiceSynchronizationJob>(
    new JobKey(
        "tenant-a",
        "billing",
        "invoice-sync"),
    JobExecutionPolicy.IgnoreFailures);
```

---

# Architecture at a Glance

```text
+------------------------------------------------+
| Application                                    |
+------------------------------------------------+
                    |
                    v
+------------------------------------------------+
| JobGuardian Hosted Service                     |
+------------------------------------------------+
                    |
                    v
+------------------------------------------------+
| Job Execution Coordinator                      |
+------------------------------------------------+
         |                          |
         v                          v
+------------------+      +----------------------+
| Lease Management |      | State Management     |
+------------------+      +----------------------+
         |                          |
         +------------+-------------+
                      |
                      v
+------------------------------------------------+
| Persistence Provider                           |
+------------------------------------------------+
          |                          |
          v                          v
+------------------+      +----------------------+
| InMemory         |      | PostgreSQL           |
+------------------+      +----------------------+
```

---

# Documentation

## Getting Started

- docs/QUICKSTART.md

## Architecture

- docs/architecture/Architecture-Overview.md

## Protocol

- docs/protocol/protocol-v1.md

## Contract Tests

- docs/contracts/contract-tests-v1.md

## Architecture Decision Records

- docs/adr/

Key ADRs:

- ADR-006 Runtime Architecture
- ADR-011 Failure Handling and Execution Outcome Model
- ADR-012 Job State Model
- ADR-013 Persistent Job State Storage
- ADR-014 Dependency Injection and Provider Registration Model

---

# Current Status

Current MVP capabilities:

- distributed lease coordination
- heartbeat-based ownership renewal
- execution ownership enforcement
- failure policy management
- runtime state management
- persistent blocked states
- manual reset workflows
- InMemory provider
- PostgreSQL provider
- fluent provider registration

Current automated test coverage:

```text
94 passing tests
```

---

# Contributing

See:

```text
CONTRIBUTING.md
```

---

# Security

See:

```text
SECURITY.md
```

---

# Code of Conduct

See:

```text
CODE_OF_CONDUCT.md
```

---

# License

Licensed under the MIT License.

See:

```text
LICENSE
```