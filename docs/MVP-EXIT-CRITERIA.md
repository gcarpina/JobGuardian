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
- The current implementation targets .NET 8 and .NET 10. JobGuardian continues compiling and testing .NET 8 for now, but does not promise fixes after Microsoft's end-of-support date; removing that target requires a prior product decision. Other language SDKs are not part of this MVP
- The in-memory provider coordinates only within one process and loses lease and state data when the process exits
- PostgreSQL is required for coordination and persistent blocked state across application instances
- PostgreSQL schema provisioning is an operator responsibility; the provider does not apply migrations
- The current failure policies are `Ignore` and `RequireManualReset`; bounded callback retries are opt-in and default to one total attempt
- Execution history is queryable by tenant/job, bounded time range and outcome; retention and deletion remain consumer/operator responsibilities
- The .NET Core runtime emits execution and lease metrics/traces through `Meter` and `ActivitySource`; SDK/exporter setup, state gauges, and administrative instrumentation remain host/follow-up responsibilities
- Audit behavior, dashboards and additional database providers are outside the MVP
- The initial PostgreSQL schema also contains an audit table reserved for future work; its presence does not indicate runtime support

## Release Gate

The MVP implementation can be treated as complete for the current preview. Before publishing a
package release, separately verify that:

- the product decisions are recorded in [ADR-015](./adr/ADR-015-JobGuardian-1.0-Contract-Ratification.md)
- all package artifacts have the intended `0.9.0-preview1` version and metadata
- a clean consumer application can restore the published packages and compile the documented setup
- PostgreSQL schema scripts V001 and V002 are available to package consumers and applied in order
- PostgreSQL schema provisioning and the manual-reset flow have been exercised against the intended deployment environment
- release notes communicate the breaking change from `Task<bool>` to `Task<ExecutionResult>`

Do not infer API stability or production readiness from passing tests alone.

The target-framework matrix must be checked against [Microsoft's .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) before each release. Build compatibility after Microsoft's end-of-support date is not a commitment to provide fixes. Discuss before removing .NET 8 from builds and tests.
