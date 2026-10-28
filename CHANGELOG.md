# Changelog

Notable changes to JobGuardian are documented here.

## [0.9.1-preview1] - Unreleased

This is a preview release, not a stable 1.0 release. See the
[preview release notes](./docs/RELEASE-NOTES-0.9.1-preview1.md) for consumer-facing highlights,
breaking changes, setup guidance, and known limitations.

### Added

- PostgreSQL execution-history queries filtered by tenant/job, time range, and outcome, with pagination
- Recovery classification for lease-acquired executions without a terminal history outcome
- Optional host-provided execution correlation IDs, with generated IDs when none are supplied
- Auditing of accepted programmatic reset requests before state mutation; a stable actor ID is required from the host
- A single transactional PostgreSQL bootstrap schema for new databases
- Contract and integration tests for recovery, history queries, correlation, reset auditing, and execution outcomes

### Changed

- Ratified the .NET protocol and contract-test behavior for execution outcomes, recovery, bounded retries, correlation, telemetry, and framework support policy
- `RequireManualReset` blocks after `Failed`, `LeaseLost`, and recovered `Abandoned` outcomes; ordinary `Skipped` and `Cancelled` outcomes do not block
- Lease and heartbeat durations are validated before execution, and memory/PostgreSQL lease-expiry comparisons use the same inclusive boundary
- .NET 10 is the recommended runtime. .NET 8 remains a conditional build and test target while technically compatible; fixes after Microsoft's end-of-support date are not promised
- CI enforces line coverage above 80% for each production package and branch coverage above 80% where applicable

### Breaking Changes

- `IJobExecutionCoordinator.ExecuteAsync` returns `ExecutionResult` instead of `bool`
- `IJobStateManager.ResetAsync` requires a stable actor ID supplied by the host
- `IExecutionHistoryStore` adds required paginated-query and lease-acquisition/recovery operations; custom implementations must be updated
- Applications processing persisted execution history should handle the `Abandoned` outcome and `ExecutionOutcomeReason` values

### PostgreSQL Setup and Upgrade

For a new database, apply `V001_initial_schema.sql` from the package. It creates the complete schema
in one transaction; JobGuardian does not apply it automatically. This preview does not provide an
upgrade path from databases initialized by earlier previews. The script is not rerunnable; use a
fresh database unless you have independently prepared and validated a data-preserving migration.
See the [PackageConsumer guide](./dotnet/samples/PackageConsumer/README.md) for locating the
packaged SQL file.

### Reset Audit Semantics

The reset API requires a stable actor ID supplied by the hosting application. Authentication and
authorization remain host responsibilities. Audit persistence occurs before the state update: a
failed audit write prevents the reset, while a later state-write failure leaves a truthful
`ResetJobRequested` event. The event records an accepted request, not proof that the state change
completed. PostgreSQL audit records are durable; InMemory records are volatile. Retention and
deletion remain host/operator responsibilities.

### Release Validation

- Added package-consumer validation in CI for restore, compilation, dependency injection, and PostgreSQL operations against the packaged schema
- Added a PostgreSQL integration test that checks the bootstrap schema shape against the initialized database
- Added a formal preview release checklist in [MVP exit criteria](./docs/MVP-EXIT-CRITERIA.md#release-checklist-preview-gate)
