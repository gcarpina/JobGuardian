# ADR-014 - Dependency Injection and Provider Registration Model

**Status:** Approved

**Date:** 2026-09-18

## Executive Summary

JobGuardian requires a simple and extensible mechanism to configure the framework and its infrastructure providers.

The adopted solution introduces a dedicated builder (`JobGuardianBuilder`) returned by `AddJobGuardian()`, allowing providers to extend the framework through a fluent registration model.

The objective is to:

- provide a simple onboarding experience
- support pluggable infrastructure providers
- allow the Core package to be used without external dependencies
- enable future framework extensions without breaking the public API

---

# Context

The framework requires:

- a minimal configuration usable out of the box
- a default state repository for simple scenarios
- the ability to replace infrastructure components through providers
- an extensible model that can evolve without breaking backward compatibility

The following alternatives were considered.

## Option A

Register everything directly through `IServiceCollection` extensions.

```csharp
services.AddJobGuardian();

services.UsePostgreSql(
    connectionString);
```

## Option B

Introduce a dedicated builder.

```csharp
services
    .AddJobGuardian()
    .UsePostgreSql(
        connectionString);
```

---

# Decision

Option B was selected.

`AddJobGuardian()` returns a `JobGuardianBuilder` instance that becomes the official extension point for the JobGuardian ecosystem.

---

# Final API

## Minimal Configuration

```csharp
services.AddJobGuardian();
```

Resulting configuration:

```text
IJobStateRepository
    ->
InMemoryJobStateRepository
```

No database dependency is required.

---

## PostgreSQL Configuration

```csharp
services
    .AddJobGuardian()
    .UsePostgreSql(
        connectionString);
```

Resulting configuration:

```text
IPostgreSqlConnectionFactory
    ->
PostgreSqlConnectionFactory

ILeaseStore
    ->
PostgreSqlLeaseStore

IJobStateRepository
    ->
PostgreSqlJobStateRepository
```

---

## Advanced Configuration

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

This overload provides future extensibility while preserving immutable options.

---

# JobGuardianBuilder

The builder is defined within the Core package.

```csharp
public sealed class JobGuardianBuilder
{
    public IServiceCollection Services
    {
        get;
    }

    public JobGuardianBuilder(
        IServiceCollection services)
    {
        Services = services;
    }
}
```

---

# Default Repository Model

The Core package registers an in-memory repository by default.

```text
AddJobGuardian()
        ↓
InMemoryJobStateRepository
```

Rationale:

- zero-configuration onboarding
- simpler testing scenarios
- no infrastructure dependency required

---

# Provider Override Model

Providers may replace infrastructure services registered by the Core package.

Example:

```csharp
services
    .AddJobGuardian()
    .UsePostgreSql(
        connectionString);
```

Result:

```text
IJobStateRepository
        ↓
PostgreSqlJobStateRepository
```

This behavior relies on the standard Microsoft Dependency Injection resolution model, where the last registration wins when resolving a service.

---

# Options Model

The framework adopts the standard .NET options pattern.

```csharp
IOptions<PostgreSqlOptions>
```

Rationale:

- alignment with .NET conventions
- future configuration extensibility
- separation between public API and implementation details

---

## PostgreSqlOptions

Current version:

```csharp
public sealed class PostgreSqlOptions
{
    public required string ConnectionString
    {
        get;
        init;
    }
}
```

Options are immutable.

---

# Validation Rules

The following configurations are considered invalid:

```csharp
.UsePostgreSql(null)
```

```csharp
.UsePostgreSql("")
```

```csharp
.UsePostgreSql("   ")
```

In these cases, an `ArgumentException` must be thrown.

---

# Architectural Consequences

## Advantages

### Improved Developer Experience

Minimal setup:

```csharp
services.AddJobGuardian();
```

Persistent configuration:

```csharp
services
    .AddJobGuardian()
    .UsePostgreSql(
        connectionString);
```

---

### Progressive Adoption

Applications may start with:

```text
InMemory
```

and later migrate to:

```text
PostgreSQL
```

without changing application-level code.

---

### Future Extensibility

Additional providers can be introduced without modifying the Core package.

```csharp
.UseSqlServer()

.UseMongoDb()

.UseRedis()
```

---

## Disadvantages

### Dependency Direction

The PostgreSql package depends on the Core package in order to consume:

```csharp
JobGuardianBuilder
```

This dependency is considered acceptable because PostgreSql is an official provider of the framework.

---

# Alternatives Rejected

## Direct IServiceCollection Extensions

The following model was rejected:

```csharp
services.AddJobGuardian();

services.UsePostgreSql(
    connectionString);
```

### Reasons

#### Lack of a Dedicated Extension Point

Every new provider would introduce additional extension methods directly on `IServiceCollection`.

Example:

```csharp
services.UsePostgreSql();

services.UseSqlServer();

services.UseMongoDb();

services.UseRedis();
```

Over time this would dilute the JobGuardian configuration surface and make framework-specific extensions harder to identify.

---

#### Reduced Discoverability

With a builder-based API, all framework extensions naturally start from:

```csharp
services
    .AddJobGuardian()
```

This improves discoverability and makes the public API easier to learn.

---

#### Lower Long-Term Scalability

The builder provides a dedicated expansion point for future framework capabilities such as:

```csharp
.UsePostgreSql()

.UseSqlServer()

.UseMongoDb()

.UseRedis()

.UseExecutionHistory()

.UseAuditing()
```

without polluting the global `IServiceCollection` extension space.

---

#### Weaker Framework Identity

A dedicated builder clearly communicates that the following configuration belongs to the JobGuardian ecosystem.

```csharp
services
    .AddJobGuardian()
    .UsePostgreSql(
        connectionString);
```

This creates a more cohesive and recognizable API surface.

---

# Verification

Compliance with this decision is automatically verified by the following tests.

## Builder Registration

```text
CT1200
AddJobGuardian_Should_Return_JobGuardianBuilder

CT1210
AddJobGuardian_Should_Still_Register_Hosted_Service

CT1220
AddJobGuardian_Should_Return_Builder_With_Same_ServiceCollection
```

## Provider Registration

```text
CT1230
UsePostgreSql_Should_Register_LeaseStore

CT1240
UsePostgreSql_Should_Register_JobStateRepository

CT1250
UsePostgreSql_Should_Register_ConnectionFactory

CT1260
UsePostgreSql_Should_Override_InMemory_StateRepository

CT1270
UsePostgreSql_Should_Register_Options

CT1280
UsePostgreSql_Should_Reject_Empty_ConnectionString
```

---

# Fitness Functions

Compliance with this ADR is objectively verified by the execution of tests:

```text
CT1200 - CT1280
```

Successful execution of these tests demonstrates that the provider registration model behaves according to the architectural decision.

---

# Decision Outcome

**Approved**

The `JobGuardianBuilder`-based configuration model is adopted as the official framework extension mechanism for version 1.0 and future releases.