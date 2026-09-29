# Changelog

Notable changes to JobGuardian are documented here.

## [0.9.0-preview1] - Unreleased

### Added

- `JobExecutionPolicy.FailurePolicy` configures how execution failures affect future job eligibility
- End-to-end coverage for failure handling, blocked jobs, manual reset, and PostgreSQL state persistence

### Changed

- `IJobExecutionCoordinator.ExecuteAsync` now returns `Task<ExecutionResult>` instead of `Task<bool>`
- `Skipped` outcomes leave the job state unchanged; `RequireManualReset` blocks jobs after `Failed` or `LeaseLost` outcomes

### Migration

This preview changes the public coordinator contract. Consumers that call or implement
`IJobExecutionCoordinator` must update their code to use `ExecutionResult` and inspect its
`Outcome` rather than treating the return value as a boolean. The old and new signatures cannot
coexist under the same method name because C# does not support overloads distinguished only by
return type.

Set `FailurePolicy` on `JobExecutionPolicy` to `FailurePolicy.RequireManualReset` to block a job
after a failure until it is reset. The default is `FailurePolicy.Ignore`.
