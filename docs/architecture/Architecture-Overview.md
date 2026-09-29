# JobGuardian Architecture

## Executive Summary

JobGuardian is a cloud-native friendly distributed job coordination framework.

Its primary objective is to guarantee single-owner execution of recurring jobs across multiple application instances.

The framework provides:

- lease-based distributed coordination
- heartbeat-based ownership renewal
- failure policy management
- runtime state management
- persistent blocked state handling
- manual reset workflows
- optional persistence providers

JobGuardian is designed for workloads where concurrent execution of the same logical job must be prevented.

Typical examples include:

- billing processes
- invoice generation
- periodic synchronization
- scheduled imports
- maintenance operations
- data reconciliation jobs

---

# Problem Statement

In a distributed environment, multiple application instances may attempt to execute the same job.

Without coordination, this leads to:

- duplicate processing
- data corruption
- race conditions
- inconsistent results
- operational failures

Example:

```text
Instance A
    ↓
Invoice Generation

Instance B
    ↓
Invoice Generation
```

Result:

```text
Duplicate invoices
```

JobGuardian ensures that only a single instance owns and executes a job at any given time.

---

# What JobGuardian Is

JobGuardian is a distributed job coordination framework.

It is responsible for:

- execution ownership
- lease management
- failure handling
- runtime state management

JobGuardian is intentionally focused on execution coordination rather than scheduling.

Scheduling remains the responsibility of the hosting application or an external scheduler.

Examples:

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

# Cloud-Native Characteristics

JobGuardian is designed to be cloud-native friendly.

Key characteristics include:

- stateless runtime components
- externalized persistence
- distributed lease-based coordination
- support for multi-instance deployments
- provider-based infrastructure abstraction
- container-friendly execution model

JobGuardian does not depend on Kubernetes-specific APIs and can run in any environment capable of hosting applications.

---

# Architecture Overview

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

# End-to-End Execution Flow

The following diagram illustrates the complete runtime lifecycle of a job execution.

```text
┌─────────────────────┐
│ JobGuardianHosted   │
│ Service             │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Resolve Job         │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Acquire Lease       │
└──────────┬──────────┘
           │
           ▼
      Lease Acquired?
           │
    ┌──────┴──────┐
    │             │
    ▼             ▼
  No             Yes
    │             │
    │             ▼
    │      ┌─────────────────┐
    │      │ Start Heartbeat │
    │      └────────┬────────┘
    │               │
    │               ▼
    │      ┌─────────────────┐
    │      │ Execute Job     │
    │      └────────┬────────┘
    │               │
    │               ▼
    │      ┌─────────────────┐
    │      │ Execution       │
    │      │ Outcome         │
    │      └────────┬────────┘
    │               │
    │               ▼
    │      ┌─────────────────┐
    │      │ Failure Policy  │
    │      │ Evaluation      │
    │      └────────┬────────┘
    │               │
    │               ▼
    │      ┌─────────────────┐
    │      │ Update Job      │
    │      │ State           │
    │      └────────┬────────┘
    │               │
    │               ▼
    │      ┌─────────────────┐
    │      │ Stop Heartbeat  │
    │      └────────┬────────┘
    │               │
    │               ▼
    │      ┌─────────────────┐
    │      │ Release Lease   │
    │      └────────┬────────┘
    │               │
    └───────────────┘
                    ▼
              Next Poll Cycle
```

## Key Observations

- lease ownership determines whether execution is allowed
- heartbeat maintains ownership while a job is executing
- execution outcomes are converted into policy decisions
- policy decisions drive runtime state transitions
- state is persisted through the configured repository
- lease ownership is always released when execution completes

---

# Core Concepts

## Job

A job is a unit of executable business logic.

Example:

```csharp
public sealed class InvoiceSynchronizationJob
    : IJob
{
    public Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        ...
    }
}
```

---

## JobKey

A JobKey uniquely identifies a logical job.

```text
TenantId
JobNamespace
JobName
```

Example:

```text
default
billing
invoice-sync
```

---

## Lease

A lease represents temporary ownership of a job.

Only the lease owner may execute the job.

```text
Lease Acquired
        ↓
Execution Allowed
```

```text
Lease Rejected
        ↓
Execution Skipped
```

---

## Execution Identity

Each running instance exposes a unique execution identity.

Example:

```text
production/webapi-01/instance-03
```

The identity is stored together with lease ownership information.

---

## Failure Policy

A failure policy determines how JobGuardian reacts to execution failures.

Supported policies:

```text
Ignore

RequireManualReset
```

---

## Runtime State

JobGuardian tracks execution eligibility through runtime states.

Supported states:

```text
Eligible

Blocked
```

# Execution Flow

The following diagram illustrates the execution lifecycle.

```text
Hosted Service
        ↓
Resolve Job
        ↓
Acquire Lease
        ↓
Lease Acquired?
        ↓
+----------------------+
| Yes                  |
+----------------------+
        ↓
Start Heartbeat
        ↓
Execute Job
        ↓
Execution Result
        ↓
Policy Evaluation
        ↓
State Transition
        ↓
Release Lease
```

If acquisition fails:

```text
Acquire Lease
        ↓
Rejected
        ↓
Execution Skipped
```

---

# Lease Management

The lease management subsystem guarantees single-owner execution.

Responsibilities:

- lease acquisition
- lease renewal
- lease expiration
- lease release
- ownership verification

---

## Lease Acquisition

```text
No Active Owner
        ↓
Acquire
        ↓
Owner Assigned
```

Only one owner may successfully acquire a lease at any given time.

For details see:

```text
ADR-008
ADR-009
```

---

## Lease Renewal

While a job executes, a heartbeat periodically renews ownership.

```text
Acquire
    ↓
Heartbeat
    ↓
Heartbeat
    ↓
Heartbeat
```

This prevents lease expiration during long-running executions.

---

## Lease Loss

If ownership cannot be renewed:

```text
Heartbeat Failure
        ↓
Lease Lost
        ↓
Execution Cancelled
```

This prevents execution from continuing without valid ownership.

The framework treats lease ownership as a hard requirement for execution.

---

# Failure Handling

JobGuardian decouples execution outcomes from runtime state management.

Execution outcomes are evaluated through failure policies.

This separation allows the framework to:

- distinguish execution results from business decisions
- support multiple failure strategies
- evolve policies independently from execution logic

For details see:

```text
ADR-011
```

---

## Execution Outcomes

Supported outcomes:

```text
Succeeded

Failed

Cancelled

Skipped

LeaseLost
```

These outcomes are generated by the execution coordinator and evaluated by the failure policy subsystem.

---

## Ignore

Failures do not modify the runtime state.

```text
Failure
    ↓
Eligible
```

The job remains executable.

Recommended for:

- failures that should remain eligible for a later poll
- non-critical recurring workloads

---

## RequireManualReset

Failures transition the job into a blocked state.

```text
Failure
    ↓
Blocked
```

The job becomes non-executable until manually reset.

Recommended for:

- critical workloads
- financial operations
- reconciliation processes
- workloads requiring operator review

---

# State Management

JobGuardian separates execution from state persistence.

Responsibilities:

- state evaluation
- state transitions
- state persistence
- eligibility checks

The state subsystem controls whether a job may execute.

For details see:

```text
ADR-012
ADR-013
```

---

## State Model

Current state model:

```text
Eligible

Blocked
```

Runtime execution temporarily moves through an internal Running state, but persistent state management is focused on execution eligibility.

---

## Eligible

The job is allowed to execute.

```text
Eligible
    ↓
Execution Allowed
```

This is the default state.

---

## Blocked

Execution is prevented.

```text
Blocked
    ↓
Execution Skipped
```

Blocked jobs require manual intervention before execution can resume.

---

# State Transition Model

Supported transitions:

## Failure With Manual Reset Policy

```text
Eligible
    ↓
Failure
    ↓
Blocked
```

---

## Manual Reset

```text
Blocked
    ↓
Reset
    ↓
Eligible
```

Unsupported transitions are rejected by the state transition engine.

---

# Persistent State Storage

JobGuardian persists only non-default runtime states.

Current persisted state:

```text
Blocked
```

Default state:

```text
Eligible
```

is represented by the absence of a persisted record.

This approach minimizes storage requirements and simplifies state management.

For details see:

```text
ADR-013
```

---

## Example

Persisted:

```text
Blocked
```

Not Persisted:

```text
Eligible
```

Result:

```text
Smaller storage footprint

Simpler queries

Simpler recovery logic
```

---

# Manual Reset Flow

Manual reset restores execution eligibility.

```text
Blocked
    ↓
Reset
    ↓
Eligible
```

Implementation:

```csharp
await jobStateManager.ResetAsync(
    jobKey,
    cancellationToken);
```

Manual reset is intentionally explicit and operator-driven.

---

# Persistence Layer

JobGuardian separates runtime behavior from persistence technology.

Current providers:

```text
InMemory

PostgreSQL
```

This architecture allows additional providers to be introduced without modifying runtime behavior.

Responsibilities:

- lease persistence
- runtime state persistence

This separation is one of the key cloud-native friendly characteristics of the framework.

---

# Provider Architecture

```text
Runtime Components
        ↓
Abstractions
        ↓
Provider Implementation
```

Current provider implementations:

```text
InMemory

PostgreSQL
```

Potential future providers:

```text
SQL Server

MySQL

Redis
```

The runtime layer remains independent from provider implementations.

---

# Database Schema

## Active Executions

Table:

```text
jobguardian_active_executions
```

Stores:

- current lease ownership
- lease expiration
- heartbeat timestamps
- execution identity

Responsibilities:

```text
Lease ownership

Ownership verification

Heartbeat management
```

---

## Runtime State

Table:

```text
jobguardian_job_state
```

Stores:

- blocked state
- state update timestamp

Responsibilities:

```text
Execution eligibility

Manual reset support

Persistent state tracking
```

# Dependency Injection Model

JobGuardian uses a dedicated builder-based configuration model.

The builder acts as the official extension point for framework capabilities and infrastructure providers.

For details see:

```text
ADR-014
```

---

## Core Configuration

The minimal framework configuration is:

```csharp
services.AddJobGuardian();
```

This registers:

- JobGuardianHostedService
- runtime services
- state management services
- default provider implementations

No external infrastructure is required.

This configuration is suitable for:

- development
- testing
- single-node deployments

---

## PostgreSQL Configuration

For distributed execution across multiple application instances:

```csharp
services
    .AddJobGuardian()
    .UsePostgreSql(
        connectionString);
```

Alternative configuration:

```csharp
services
    .AddJobGuardian()
    .UsePostgreSql(
        new PostgreSqlOptions
        {
            ConnectionString =
                connectionString
        });
```

This enables persistent coordination through PostgreSQL.

---

## Job Registration

Jobs are registered independently from provider configuration:

```csharp
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Models;

services.AddJob<MyJob>(
    jobKey,
    new JobExecutionPolicy
    {
        LeaseDuration = TimeSpan.FromMinutes(1),
        HeartbeatInterval = TimeSpan.FromSeconds(10),
        FailurePolicy = FailurePolicy.RequireManualReset
    });
```

This separation keeps workload registration independent from infrastructure concerns.

---

# Default Provider Behaviour

Calling:

```csharp
services.AddJobGuardian();
```

registers:

```text
InMemoryLeaseStore
InMemoryJobStateRepository
```

Characteristics:

- zero infrastructure requirements
- fast local execution
- ideal for development
- ideal for automated testing

The framework is fully functional without PostgreSQL for single-process scenarios.

---

# Provider Override Behaviour

Calling:

```csharp
services
    .AddJobGuardian()
    .UsePostgreSql(
        connectionString);
```

replaces the default implementations with:

```text
IPostgreSqlConnectionFactory
    →
PostgreSqlConnectionFactory

ILeaseStore
    →
PostgreSqlLeaseStore

IJobStateRepository
    →
PostgreSqlJobStateRepository
```

Provider registrations intentionally override the default implementations registered by the Core package.

This allows infrastructure concerns to evolve independently from runtime behavior.

---

# Related ADRs

The following architectural decisions describe the evolution of the framework.

## Foundations

- ADR-001 - Protocol First
- ADR-002 - Database Time Authority
- ADR-003 - Multi-Tenant Model
- ADR-005 - Observability

## Runtime

- ADR-006 - Runtime Architecture
- ADR-008 - Atomic Lease Acquisition
- ADR-009 - Ownership Rules
- ADR-010 - Runtime Scheduling Strategy

## Failure Handling and State Management

- ADR-011 - Failure Handling and Execution Outcome Model
- ADR-012 - Job State Model
- ADR-013 - Persistent Job State Storage

## Configuration and Extensibility

- ADR-014 - Dependency Injection and Provider Registration Model

---

# When To Use JobGuardian

JobGuardian is a good fit when:

- multiple application instances may execute the same workload
- duplicate execution must be prevented
- execution ownership must be explicitly controlled
- workload execution requires failure policies
- manual intervention workflows are required
- distributed coordination is needed without adopting a larger workflow orchestration platform

Typical examples:

- invoice synchronization
- billing jobs
- recurring maintenance
- tenant data synchronization
- recurring integrations
- reconciliation workloads
- recurring data processing

---

# When Not To Use JobGuardian

JobGuardian is not intended to be:

- a scheduler
- a workflow engine
- a process orchestration platform
- a BPM solution

If the primary problem is scheduling, existing scheduling technologies may be more appropriate.

Examples:

```text
Scheduling
    ↓
Cron
Quartz
Hangfire
```

JobGuardian focuses on:

```text
Execution Ownership

Execution Coordination

Failure Policies

Runtime State Management
```

---

# Architectural Principles

The framework is built around the following principles.

## Single Ownership

Only one execution owner may hold a lease at any given time.

---

## Explicit Ownership

Execution is allowed only when lease ownership has been successfully established.

---

## Explicit Failure Handling

Execution failures are governed through formal failure policies.

---

## Explicit Recovery

Blocked jobs require an intentional recovery action before execution may resume.

---

## Provider-Based Extensibility

Infrastructure concerns are implemented through providers rather than embedded in the runtime.

---

## Cloud-Native Friendly Design

The framework is designed to support:

- containerized deployments
- multi-instance environments
- externalized state
- infrastructure abstraction
- progressive adoption of persistence providers

without requiring Kubernetes-specific integrations.

---

## Operational Simplicity

The framework should remain usable with minimal configuration while supporting more advanced deployment scenarios through providers.

---

# Current MVP Scope

The current MVP includes:

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
- automated integration testing

Current automated test coverage:

```text
106 tests
```

---

# Future Evolution

Potential post-MVP capabilities include:

- execution history
- audit trail
- SQL Server provider
- MySQL provider
- OpenTelemetry integration
- metrics exporters
- operational dashboards
- execution analytics
- additional persistence providers

These capabilities are intentionally outside the current MVP scope.

---

# Summary

JobGuardian combines:

- distributed lease coordination
- execution ownership enforcement
- heartbeat-based ownership renewal
- runtime state management
- failure policy enforcement
- optional persistence providers
- cloud-native friendly deployment characteristics

to provide a reliable foundation for recurring workload execution in distributed environments.

The framework intentionally focuses on execution coordination rather than scheduling, allowing teams to combine JobGuardian with their preferred scheduling mechanisms while maintaining strict ownership guarantees.