# JobGuardian

A cloud-native friendly framework for distributed job coordination.

JobGuardian provides lease-based execution ownership, failure policy enforcement, runtime state management, and optional persistence providers, allowing multiple application instances to coordinate recurring workloads with a single valid lease owner per workload at a time.

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

JobGuardian coordinates a single valid lease owner per workload at a time. It does not guarantee
exactly-once processing and cannot prevent duplicate side effects if a callback continues after
losing its lease.

---

# Key Features

- Distributed lease-based execution ownership
- Lease-based single-owner coordination
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
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
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
- [ADR-015: 1.0 contract ratification](./docs/adr/ADR-015-JobGuardian-1.0-Contract-Ratification.md)

## Contract Tests

- docs/contracts/contract-tests-v1.md

## Architecture Decision Records

- docs/adr/

Key ADRs:

- ADR-015 JobGuardian 1.0 Contract Ratification
- ADR-006 Runtime Architecture
- ADR-011 Failure Handling and Execution Outcome Model
- ADR-012 Job State Model
- ADR-013 Persistent Job State Storage
- ADR-014 Dependency Injection and Provider Registration Model

---

# Current Status

The current package line is `0.9.1-preview1`. The MVP implementation is feature-complete for the
preview scope; this does not mean that version 1.0 has been released or that the public API is
stable. See the
[preview release notes](./docs/RELEASE-NOTES-0.9.1-preview1.md),
[MVP exit criteria](./docs/MVP-EXIT-CRITERIA.md), and [changelog](./CHANGELOG.md).

.NET 10 is the recommended runtime. The packages continue to target and test .NET 8 while it
remains technically compatible with the code, dependencies, and build tooling; compatibility may
be reconsidered if that target causes problems. JobGuardian does not promise fixes for .NET 8 after
Microsoft's end-of-support date. See the
[framework support policy](./docs/protocol/protocol-v1.md#9-versioning-security-and-support).

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
- paginated PostgreSQL execution-history queries and lease-expiry recovery classification

Current automated test coverage:

```text
154 test cases per target framework
```

CI also enforces line coverage above 80% for each production package and branch coverage above 80%
where conditional branches are present. See [contributor testing guidance](./CONTRIBUTING.md).

## Known Limitations

JobGuardian `0.9.1-preview1` is an evaluation preview. It is not a stable 1.0 release, and its
public API and persistence contract may change in a later preview or release.

- JobGuardian coordinates execution ownership through leases; it does not schedule jobs or guarantee exactly-once processing
- A callback that continues after lease loss can still produce duplicate external side effects; callbacks should honor cancellation, and consumers should use idempotency or fencing where needed
- The InMemory provider is process-local and volatile; use PostgreSQL for coordination across application instances and persistent state
- PostgreSQL schema provisioning is operator-managed. The packaged `V001_initial_schema.sql` is for a new database, is not rerunnable, and does not upgrade databases created by earlier previews
- Reset auditing records an accepted reset request before the state update; it is not proof that the state change completed. Authentication, authorization, retention, and audit-log access remain host responsibilities
- InMemory audit records are volatile; PostgreSQL audit records are durable, but retention and deletion remain the operator's responsibility
- JobGuardian does not include a dashboard, built-in scheduler, or Java/Python SDKs
- .NET 10 is the recommended runtime. .NET 8 remains a conditional build and test target while technically compatible; fixes after Microsoft's end-of-support date are not promised

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