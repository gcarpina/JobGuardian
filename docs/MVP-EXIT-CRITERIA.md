# MVP Exit Criteria

## Assessment

The repository implementation is feature-complete for the MVP preview scope. This is not a
declaration that version 1.0 is released, that the public API is stable, or that a production
deployment has been approved.

## Exit Criteria

| Criterion | Evidence | Status |
|---|---|---|
| Single-owner execution is enforced through leases and heartbeat renewal | Core tests and PostgreSQL lease integration tests | Met |
| Execution outcomes are classified and applied to failure policy and job state | Coordinator, state-manager and hosted-service tests | Met |
| `Ignore` and `RequireManualReset` behavior is verified, including reset and persistent blocking | Core and PostgreSQL end-to-end execution-state tests | Met |
| In-memory and PostgreSQL providers are registered and usable in their intended scopes | Dependency-injection tests and PostgreSQL integration tests | Met |
| The consumer guide explains setup, provider boundaries, schema provisioning and reset | [Quick Start](./QUICKSTART.md) | Met |
| Accepted ADRs distinguish implemented scope from deferred capabilities | ADR-011 through ADR-015 | Met |
| The complete automated suite passes on each currently supported target framework | `dotnet test dotnet/JobGuardian.sln --no-restore` | Met |
| The public coordinator API change has migration guidance | [Changelog](../CHANGELOG.md) | Met |
| Callback retries are finite, configurable, and preserve a single final execution outcome | Coordinator and hosted-service tests; [ADR-011](./adr/ADR-011-Failure-Handling-And-Execution-Outcome-Model.md) | Met |

## Supported Scope and Boundaries

- JobGuardian coordinates execution; it does not schedule jobs
- .NET 10 is the recommended runtime. .NET 8 remains a build and test target while technically compatible; compatibility may be reconsidered if it causes problems. JobGuardian does not promise fixes after Microsoft's end-of-support date. Other language SDKs are not part of this MVP
- The in-memory provider coordinates only within one process and loses lease and state data when the process exits
- PostgreSQL is required for coordination and persistent blocked state across application instances
- PostgreSQL schema provisioning is an operator responsibility; the provider does not apply migrations
- The current failure policies are `Ignore` and `RequireManualReset`; bounded callback retries are opt-in and default to one total attempt
- Execution history is queryable by tenant/job, bounded time range and outcome; retention and deletion remain consumer/operator responsibilities
- The .NET Core runtime emits execution and lease metrics/traces through `Meter` and `ActivitySource`; SDK/exporter setup and state gauges remain host responsibilities
- Programmatic reset requests are audited before state mutation; authentication, authorization, audit retention, dashboards and other administrative audit remain host/future responsibilities
- PostgreSQL audit records are durable; InMemory audit records are process-local and volatile

## Release Checklist (Preview Gate)

The MVP implementation can be treated as complete for the current preview. Before publishing a
package release, the release lead must verify all items below and retain evidence in the public
release notes or a signed internal checklist.

- the product decisions are recorded in [ADR-015](./adr/ADR-015-JobGuardian-1.0-Contract-Ratification.md)
- the package version remains `0.9.1-preview1` and the preview language is consistent across README, changelog, package metadata, and public docs
- all package artifacts have the intended version and metadata, and no release artifact claims a stable 1.0 API promise
- a clean consumer application can restore the published packages and compile the documented setup from a local or remote package feed
- the PostgreSQL bootstrap schema is the single packaged schema for new databases, and no preview upgrade path is advertised or implied
- the packaged PostgreSQL schema has been applied to a clean database in the target deployment environment and the reset flow has been exercised successfully
- the package consumer sample or equivalent consumer validation passes on the supported runtime matrix, with .NET 10 as the recommended runtime and .NET 8 treated as conditional compatibility only
- release notes clearly state the preview posture, supported scope, and the known limitations for host responsibilities, audit retention, and external side effects
- release notes communicate the breaking change from `Task<bool>` to `Task<ExecutionResult>`
- the target-framework matrix has been checked against [Microsoft's .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) and the runtime guidance has been updated if support windows change

This checklist is intentionally preview-oriented. It proves that the package is suitable for controlled evaluation, not that the project is stable for general production use.

## Release Gate

Do not infer API stability or production readiness from passing tests alone.

The target-framework matrix must be checked against [Microsoft's .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) before each release. .NET 10 is the recommended runtime. Keeping .NET 8 in the build and test matrix is conditional on continued technical compatibility and is not a commitment to provide fixes after Microsoft's end-of-support date.
