# ADR-006 - Runtime Architecture

## Status

Accepted

---

## Context

JobGuardian coordinates singleton execution across distributed environments.

Potential execution environments include:

- Console Applications
- Worker Services
- Background Services
- Windows Services
- Linux Services
- Kubernetes Jobs
- Kubernetes CronJobs

The execution environment should not impact the protocol semantics.

JobGuardian must remain independent from any specific runtime platform.

---

## Decision

JobGuardian is a runtime-agnostic execution coordination framework.

JobGuardian is not a scheduler.

Scheduling responsibilities belong to external systems.

Examples:

- Kubernetes CronJobs
- Linux Cron
- Windows Task Scheduler
- BackgroundService loops
- Enterprise schedulers
- Custom schedulers

JobGuardian focuses exclusively on:

- Lease acquisition
- Execution ownership
- Heartbeat management
- Lease expiration
- Execution history
- Failure policies
- Auditability
- Observability

---

## Runtime Categories

### One-Shot Execution

Execution starts, runs once and terminates.

Examples:

- Console Application
- Kubernetes Job
- Scheduled executable

Typical flow:

```text
Acquire
↓
Execute
↓
Release
↓
Terminate
```

---

### Long Running Runtime

Process remains alive and periodically executes work.

Examples:

- Worker Service
- BackgroundService
- Windows Service
- Linux Daemon

Typical flow:

```text
Scheduler Loop
↓
Acquire
↓
Execute
↓
Release
↓
Wait
↓
Repeat
```

---

### Clustered Runtime

Multiple instances compete for job ownership.

Examples:

- Kubernetes Deployments
- Multiple Worker replicas
- Distributed services

Typical flow:

```text
Instance A
Instance B
Instance C

↓
Acquire

↓
Single winner

↓
Execute
```

---

## Supported Runtime Packages

The reference .NET implementation should provide:

```text
JobGuardian.Console
JobGuardian.Worker
JobGuardian.Kubernetes
```

### JobGuardian.Console

Provides integration for:

- Console applications
- Scheduled executables

### JobGuardian.Worker

Provides integration for:

- BackgroundService
- Worker Service
- Hosted Service

### JobGuardian.Kubernetes

Provides integration for:

- Kubernetes Job
- Kubernetes CronJob

---

## Multi-Language Support

Protocol compliance is independent from runtime technology.

Equivalent runtimes should be supportable from:

### .NET

```text
JobGuardian.Console
JobGuardian.Worker
JobGuardian.Kubernetes
```

### Java

```text
jobguardian-java
jobguardian-spring
```

### Python

```text
jobguardian-python
```

---

## Runtime Independence

The protocol must not require:

- Kubernetes APIs
- ASP.NET Core
- Spring Framework
- Python Frameworks

SDKs may provide optional integrations.

The protocol must remain fully portable.

---

## Dashboard Independence

Job execution must not depend on the dashboard.

The following must continue working even if the dashboard is unavailable:

- Acquire
- Renew
- Release
- History
- Audit
- Metrics

The dashboard is an operational component only.

---

## Failure Handling

Runtime implementations must correctly handle:

- Process termination
- Unexpected crashes
- Network failures
- Pod deletion
- Node failure

Ownership recovery must be based on lease expiration semantics defined by the protocol.

---

## Rationale

Separating execution coordination from scheduling provides:

- Runtime portability
- Simpler architecture
- Easier SDK development
- Better cloud-native adoption
- Easier multi-language support

This approach allows JobGuardian to operate consistently across a wide range of execution environments.

---

## Consequences

### Positive

- Runtime independence
- Multi-language readiness
- Cloud-native compatibility
- No scheduler lock-in

### Negative

- External scheduler required
- Scheduling configuration lives outside JobGuardian

---

## Compliance

An implementation is compliant with JobGuardian Protocol v1 if protocol semantics remain unchanged regardless of execution runtime.

Runtime-specific integrations must not alter protocol behavior.
