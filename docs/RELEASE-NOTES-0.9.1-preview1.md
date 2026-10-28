# JobGuardian 0.9.1-preview1

**Status:** Preview, not a stable 1.0 release
**Package version:** `0.9.1-preview1`
**Recommended runtime:** .NET 10

JobGuardian 0.9.1-preview1 is an evaluation release of the .NET distributed job coordination
framework. It coordinates execution ownership through leases and provides persistent state,
execution history, recovery classification, and audited programmatic reset requests.

This preview is intended to validate the product contract and package-consumer experience. It does
not declare the public API stable or make a general production-support commitment.

## Highlights

- PostgreSQL execution-history queries filtered by tenant, job, time range, and outcome, with pagination
- Recovery classification for executions that acquired a lease but have no recorded terminal outcome
- Optional host-provided execution correlation IDs
- Programmatic reset requests audited before the job state is changed
- One PostgreSQL bootstrap script packaged for initializing a new database
- Bounded callback retries and explicit execution outcome/reason classification
- .NET 10 recommended, with .NET 8 retained as a conditional build and test target

## Breaking Changes

- `IJobExecutionCoordinator.ExecuteAsync` returns `ExecutionResult` instead of `bool`, exposing the execution outcome to callers
- `IJobStateManager.ResetAsync` requires a stable actor ID from the hosting application
- `IExecutionHistoryStore` includes required paginated-query and lease-acquisition/recovery operations; custom implementations must implement them
- Persisted execution-history consumers should handle the `Abandoned` outcome and `ExecutionOutcomeReason` values

See the [changelog](../CHANGELOG.md) for additional behavior changes.

## PostgreSQL Setup and Upgrade Notes

For a new database, apply `V001_initial_schema.sql` from the `JobGuardian.PostgreSql` package.
The provider does not apply schema changes automatically. Review the script and confirm the target
database before applying it.

This preview does not provide an upgrade path from databases initialized by earlier previews. The
bootstrap script is not rerunnable; use a fresh database unless you have independently prepared and
validated a data-preserving migration. See the
[PackageConsumer guide](../dotnet/samples/PackageConsumer/README.md) for the packaged schema
location and an example provisioning flow.

## Known Limitations

- The API and persistence contract are preview-level and may change before a stable 1.0 release
- JobGuardian coordinates leases; it does not provide scheduling or exactly-once side effects
- A callback may continue after lease loss if it does not cooperate with cancellation; use idempotent operations or application-level fencing where needed
- The InMemory provider is process-local and volatile
- Reset auditing records the accepted request, not proof that the state update completed; authentication, authorization, retention, and audit access are host/operator responsibilities
- InMemory audit records are volatile; PostgreSQL records are durable, but retention and deletion are not automated
- There is no built-in dashboard or Java/Python SDK
- .NET 8 compatibility is conditional and does not include a promise of fixes after Microsoft's end-of-support date

The [README](../README.md) contains the complete public-facing limitations section. The
[preview release checklist](./MVP-EXIT-CRITERIA.md#release-checklist-preview-gate) defines the
release verification gate.
