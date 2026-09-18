# ADR-007 - Foreign Key Strategy

## Status

Accepted

---

## Context

JobGuardian persists runtime information in multiple tables.

Examples:

- jobguardian_active_executions
- jobguardian_execution_history
- jobguardian_audit_log

Several logical relationships exist between these tables.

Examples:

- active execution references execution history through execution_id
- audit records may reference executions
- audit records may reference jobs

A traditional relational design would enforce these relationships through database foreign keys.

---

## Decision

JobGuardian does not define mandatory foreign keys between runtime tables.

Relationships are enforced through:

- Protocol semantics
- SDK behavior
- Contract tests

instead of database-level foreign key constraints.

---

## Examples

### Active Execution

The following relationship exists:

```text
jobguardian_active_executions.execution_id
```

↓

```text
jobguardian_execution_history.execution_id
```

This relationship is required by the protocol.

However, it is not enforced by a database foreign key.

---

### Audit Records

The following relationship may exist:

```text
jobguardian_audit_log.execution_id
```

↓

```text
jobguardian_execution_history.execution_id
```

This relationship is optional.

Not all audit records relate to a specific execution.

Examples:

- ForceReleaseLease
- ResetFailureState
- Administrative configuration changes

---

## Rationale

The protocol supports multiple persistence technologies.

Examples:

- PostgreSQL
- MySQL
- SQL Server
- MongoDB

Database foreign keys provide limited value for protocol correctness because:

- Relationships already exist at the protocol level
- Cleanup operations become more complex
- Retention management becomes more complex
- Cross-provider support becomes harder
- Runtime recovery scenarios become more constrained

Protocol semantics are considered the source of truth.

---

## Retention Considerations

Execution history retention is configurable.

Default retention:

```text
365 days
```

Cleanup jobs may delete historical records.

Foreign keys would introduce additional constraints and retention dependencies between tables.

Avoiding foreign keys keeps cleanup operations independent.

---

## Recovery Considerations

Runtime recovery scenarios may include:

- Process crash
- Node termination
- Pod deletion
- Infrastructure failure

The protocol should remain resilient even when partial records require reconciliation.

Avoiding foreign keys simplifies recovery operations.

---

## Contract Validation

Consistency is verified through contract tests.

Examples:

### Required

An active execution must have a corresponding execution history record.

### Required

Execution history must contain the execution metadata required by the protocol.

### Required

Audit records must contain the information required by the protocol.

These guarantees are verified by automated compliance tests rather than database constraints.

---

## Multi-Language Considerations

The same protocol semantics must behave identically across:

- .NET
- Java
- Python

Protocol-based validation is easier to implement consistently than database-specific foreign key strategies.

---

## Consequences

### Positive

- Simpler schema evolution
- Easier retention management
- Easier cleanup operations
- Better portability
- Better database compatibility
- Better support for future persistence providers

### Negative

- Referential integrity is not enforced by the database
- SDK implementations must be carefully tested
- Contract tests become more important

---

## Compliance

An implementation remains compliant if all protocol relationships are maintained correctly, even when no database foreign keys are defined.

Protocol compliance is verified by contract tests and SDK behavior, not by relational constraints.