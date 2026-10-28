# ADR-015: JobGuardian 1.0 Contract Ratification

## Status

Accepted

## Context

The Protocol v1 and Contract Tests v1 drafts did not match the .NET preview runtime. The gaps
included outcome vocabulary, abandoned execution recovery, history query and retention, retry
semantics, correlation, observability ownership, administrative scope, and framework support.
Shipping without resolving these gaps would leave users unable to distinguish SDK guarantees from
aspirational or control-plane requirements.

The product decision is to ship a focused .NET coordination SDK and PostgreSQL provider. Protocol
v1 remains language-neutral, but conformance is implementation-specific and only the .NET SDK may
claim conformance at the 1.0 release.

## Decision

1. **Product boundary:** the SDK coordinates job execution and includes a convenience hosted poller.
   It is not a general scheduler. The SDK provides programmatic state reset and records each
   accepted reset request before changing state. It provides no dashboard, manual-run endpoint,
   force takeover, authentication, or authorization. The hosting application authenticates the
   actor and supplies its stable identifier; dashboard and other administrative actions remain
   outside the SDK contract
2. **Outcomes:** use `Succeeded`, `Failed`, `Cancelled`, `LeaseLost`, `Skipped`, and `Abandoned`.
   `Skipped` carries `LeaseBusy` or `Blocked`. `Abandoned` is terminal history state assigned to a
   previously lease-acquired execution with no terminal outcome when a later owner acquires the
   expired job lease. It is an observed lack of persisted completion, not proof of process death
3. **Failure policy:** `RequireManualReset` blocks on `Failed`, `LeaseLost`, and `Abandoned`;
   `Cancelled` and ordinary `Skipped` do not block. If recovery discovers `Abandoned` after lease
   acquisition, the new callback is not invoked, the attempt is recorded as `Skipped`/`Blocked`,
   and the job remains blocked until reset
4. **History:** provide lookup by execution ID and paginated query by tenant/job, bounded start-time
   interval, and optional outcome. History is best-effort and never changes callback behavior.
   Retention and deletion are consumer/operator responsibilities; the SDK has no automatic cleanup
   or default retention promise
5. **Abandonment recovery:** after successful lease acquisition and before invoking the callback,
   mark earlier lease-acquired incomplete history entries as `Abandoned`. Persist lease acquisition
   separately so failed pre-acquisition requests are not misclassified. If the history update
   fails, report it and allow the new owner to continue; a later acquisition may retry recovery
6. **Retries:** make existing .NET semantics normative cross-SDK: `max_attempts` defaults to 1 and
   ranges from 1–10 total callback invocations; fixed delay defaults to one second and ranges from
   zero to five minutes.
   Retry only callback exceptions, use one lease/execution/correlation identity for the sequence,
   and stop immediately on cancellation or lease loss. Exhaustion yields `Failed`. Applications
   enabling retries must make external effects idempotent or deduplicate them; exactly-once is not
   promised and callback duration is not bounded by the attempt count
7. **Correlation and traces:** the host may provide a correlation ID; otherwise the SDK generates
   one. It remains stable across retries and is persisted in history when available. OpenTelemetry
   trace context is separate and follows `Activity`. The SDK emits ActivitySource/Meter signals but
   does not configure exporters or endpoints
8. **Metric cardinality/privacy:** metric attributes are low-cardinality and non-sensitive.
   Tenant/job/execution/correlation IDs and error messages are excluded from metric attributes
9. **Lease validity:** positive lease and heartbeat durations are required, with
   `heartbeat_interval < lease_duration` enforced before execution. One-third is recommended only.
   A lease is expired at `lease_until <= database_time`; renewal requires `lease_until >
   database_time`
10. **Reset audit:** every accepted programmatic reset request, including an idempotent reset, is
    written to the configured audit store before the job state is changed. If the audit write fails,
    the reset is not applied and the error is propagated. The event is `ResetJobRequested` and
    records tenant, job, UTC timestamp, and the required stable actor ID supplied by the host;
    display name and free-form reason are not required. The event records a request, not proof that
    the state change completed. PostgreSQL audit records are durable; InMemory records are volatile
11. **Framework support:** distinguish build compatibility from support. Continue compiling and
    testing `net8.0` after Microsoft's end-of-support date for now, but do not promise security or
    maintenance fixes for it after 2026-11-10. As of 2026-10-27, Microsoft supports `net10.0`
    through 2028-11-14. Reassess the target matrix against Microsoft's lifecycle before each
    release. Any removal of `net8.0` from builds and tests requires a prior product decision

## Rationale and alternatives

- **Keep the core narrow:** adding a control plane would expand security and operational ownership
  beyond the current SDK. A future control-plane contract can evolve independently
- **Do not automatically delete history:** retention requirements vary by operator and may be
  regulatory. A consumer-owned policy avoids a destructive hidden default at the cost of requiring
  operators to provision cleanup
- **Classify abandonment only after a new lease acquisition:** lease expiry alone does not prove
  process death. Acquisition proves the old owner no longer holds the lease, while the persisted
  acquisition marker avoids labeling an execution that never acquired ownership. History remains
  best-effort by explicit decision
- **Use fixed retries:** it preserves current behavior and cross-SDK predictability. Exponential
  backoff/jitter can be considered later if contention or synchronized retry load is evidenced
- **Keep exporters host-owned:** this reuses the application's observability platform and avoids
  vendor lock-in; consumers must explicitly configure listeners/exporters to see telemetry
- **Audit reset requests before state changes:** this fails closed when audit persistence is
  unavailable and leaves a truthful request record even if the subsequent state update fails;
  the two operations are not a shared transaction
- **Keep .NET 8 build compatibility without extended servicing:** this preserves short-term
  consumer compatibility while avoiding a promise to service an unsupported runtime. Consumers
  should migrate to a Microsoft-supported framework for security updates; removing the build target
  remains a separate decision

## Consequences and ownership

- Protocol and contract tests are normative for the ratified behaviors; runtime changes must pass
  the applicable tests before conformance is claimed
- The SDK team does not promise security or maintenance fixes for a target framework after
  Microsoft's end-of-support date, even if the repository continues compiling that target
- Consumers own audit retention, exporter/endpoint configuration, authentication and authorization,
  and audit of administrative operations other than SDK reset requests
- PostgreSQL schema v3 requires applying V002 and V003 after the initial schema; migrations are not
  applied automatically
- The recovery classification and the original execution outcome are separate facts. Operators
  should treat `Abandoned` as an uncertain completion state and reconcile external side effects
  idempotently

## Conditions to revisit

Revisit this decision if reset auditing must be atomic with state persistence, a control plane or
non-.NET SDK is committed for 1.0, operators require
provider-managed retention, recovery needs to classify abandoned runs without another acquisition,
retry load demonstrates a need for jitter/backoff, Microsoft's lifecycle changes a target's support
window, or before removing `net8.0` from the build and test matrix.
