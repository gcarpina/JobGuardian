# Changelog

Notable changes to JobGuardian are documented here.

## [0.9.0-preview1] - Unreleased

### Added

- Ratified the .NET 1.0 protocol and contract-test behavior, including execution outcomes, recovery, bounded retries, correlation, telemetry, and framework support policy
- PostgreSQL execution-history queries with tenant/job, time-range, outcome, and pagination filters
- Recovery classification of prior lease-acquired executions without a terminal history outcome
- Host-provided execution correlation IDs, with generated IDs when the host does not supply one
- Audit every accepted programmatic reset request before applying the state change; actor ID is required and supplied by the host
- Contract and integration tests for recovery, history queries, correlation, and the ratified outcomes

### Changed

- `RequireManualReset` now blocks after `Failed`, `LeaseLost`, and recovered `Abandoned` outcomes; ordinary `Skipped` and `Cancelled` outcomes do not block
- Lease and heartbeat durations are validated before execution, and lease-expiry comparisons use the same inclusive boundary in memory and PostgreSQL
- PostgreSQL schema versions 2 and 3 add recovery/query support and actor-ID-only reset audit records
- CI measures coverage and requires line coverage strictly above 80% per production package, plus branch coverage strictly above 80% where applicable
- JobGuardian continues compiling and testing .NET 8 for now, but does not promise fixes after Microsoft's end-of-support date; removing that target requires a prior product decision

### Migration

Apply `V002_execution_history_query_and_recovery.sql` and
`V003_audit_actor_optional_display_name.sql` after `V001_initial_schema.sql` on new databases.
Apply V002 and V003 to existing databases before deploying this version. JobGuardian does not apply
migrations automatically; see the PackageConsumer guide for applying the scripts from the NuGet
package.

The reset API now requires the stable actor ID supplied by the hosting application. This is a
breaking change for callers of `IJobStateManager.ResetAsync`; authentication and authorization remain
host responsibilities. Audit persistence occurs before the state update; a failed audit write
prevents the reset, while a later state-write failure leaves a truthful `ResetJobRequested` event.
PostgreSQL requires V003 to allow an omitted actor display name. InMemory audit records are volatile.

This version adds required members to `IExecutionHistoryStore`. Consumers with custom history-store
implementations must implement the paginated query and lease-acquisition/recovery operations.
Applications that persist execution outcomes should account for the new `Abandoned` outcome and
`ExecutionOutcomeReason` values when reading or processing history.
