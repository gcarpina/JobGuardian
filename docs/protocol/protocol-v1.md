# JobGuardian Protocol v1

Version: 1.0

Status: Ratified for the JobGuardian .NET 1.0 release

## 1. Purpose and product boundary

JobGuardian coordinates singleton execution of a tenant-scoped job using a shared persistence
provider. It is not a general-purpose scheduler. The .NET hosted poller is a convenience trigger;
production schedule definition and calendar semantics belong to an external scheduler or host.

Protocol v1 is language-neutral. At the 1.0 release, only the .NET SDK and its PostgreSQL provider
claim conformance. Another SDK may claim conformance only after it has an implementation and passes
the applicable contract tests.

The SDK includes programmatic job-state reset and records each accepted reset request in its audit
store before changing state. It does not provide a control plane, dashboard, manual-run endpoint,
force takeover, authentication, or authorization. The hosting application authenticates the actor
and supplies a stable actor ID. Applications exposing other administrative operations own their
authentication, authorization, and audit.

## 2. Identity and time

A job is identified by `tenant_id + job_namespace + job_name`. `tenant_id` is mandatory; `default`
is a valid tenant identifier. The identity is used consistently for leases, job state, history
queries, and recovery.

Lease ownership decisions use only the persistence provider's authoritative database time. Local
process clocks must not affect acquisition, renewal, release, or expiry decisions.

An execution may acquire a lease if no active lease exists or the current lease has expired. A lease
is expired when `lease_until_utc <= database_time`. Renewal is permitted only while
`lease_until_utc > database_time`, for the current execution owner. Acquisition must be atomic so
at most one owner holds a valid lease for a job.

## 3. Heartbeat and lease configuration

Each execution configures a positive `lease_duration` and positive `heartbeat_interval`. The
configuration is invalid unless `heartbeat_interval < lease_duration`; SDKs must reject invalid
configuration before executing the job. An interval no greater than one third of the lease duration
is recommended, but is not mandatory.

Heartbeats renew the lease while callback work or retry delay is in progress. If renewal fails or
ownership can no longer be confirmed, the SDK cancels the callback cooperatively and terminates the
coordinated execution as `LeaseLost`. It must not invoke another callback attempt after lease loss.

## 4. Execution lifecycle and outcomes

An execution has one stable execution ID and one terminal outcome. Callback retry attempts remain
part of that coordinated execution and do not create separate history records.

The canonical outcomes are:

| Outcome | Meaning |
|---|---|
| `Succeeded` | The callback completed successfully while the owner held the lease |
| `Failed` | All configured callback attempts ended with an exception |
| `Cancelled` | The callback stopped in response to cooperative cancellation or host shutdown |
| `LeaseLost` | The SDK could not renew or confirm its lease and stopped the callback |
| `Skipped` | The callback did not start; `outcome_reason` distinguishes `LeaseBusy` and `Blocked` |
| `Abandoned` | A previously acquired execution had no terminal outcome persisted when a later owner acquired the expired lease |

`Abandoned` is an observation about persisted state, not proof that the prior process crashed. A
network partition, database outage, or history-write failure can produce the same observation.
After a successful lease acquisition, recovery marks earlier history entries that are both marked
as having acquired a lease and still have no outcome as terminal `Abandoned` entries. Failed
best-effort history writes are logged and do not prevent the newly acquired callback from running;
an entry that could not be updated remains eligible for a later recovery attempt.

`Skipped` has stable reason codes. `LeaseBusy` means another valid owner prevented acquisition;
`Blocked` means `RequireManualReset` state prevented the attempt. Neither reason invokes the job
callback or blocks future executions by itself.

## 5. Failure policy and reset

`Ignore` leaves the job eligible after any terminal execution outcome.

`RequireManualReset` blocks subsequent scheduled attempts after `Failed`, `LeaseLost`, or
`Abandoned`. `Succeeded`, `Cancelled`, and `Skipped` do not block. A programmatic SDK reset returns
the job to an eligible state and requires a non-empty stable actor ID from the host. Each accepted
request, including an idempotent reset, is recorded as `ResetJobRequested` before changing state.
The event stores tenant, job identity, UTC request timestamp, and actor ID; actor display name and
free-form reason are optional and omitted by the SDK. If audit persistence fails, the reset is not
applied and the error is propagated. The event records the request, not successful completion of
the state change. PostgreSQL audit entries are durable; InMemory audit entries are lost when the
process exits. The host remains responsible for authentication, authorization, and audit retention.

When an `Abandoned` entry is discovered during a later successful acquisition, the SDK persists the
blocked state and does not invoke that newly acquired callback when `RequireManualReset` is
configured. The coordinated attempt is recorded as `Skipped` with reason `Blocked`, and its lease
is released. Later poller attempts remain `Skipped`/`Blocked` until the host resets the state.
Recovery does not claim that lease acquisition and history/state updates are one transaction.

## 6. Bounded callback retries

Retry settings are part of Protocol v1 and must have equivalent semantics across conforming SDKs.
`max_attempts` counts the initial callback invocation and all retries; valid values are 1 through
10 and the default is 1. The fixed delay between failed attempts is configurable from zero through
five minutes and defaults to one second; exponential backoff and jitter are not part of v1.

Only a callback exception can trigger another attempt. Cancellation and `LeaseLost` terminate
immediately without retry. When all attempts fail, the coordinated outcome is `Failed`. All
attempts and delays use the same execution ID, correlation ID, and lease.

An exception does not prove that external side effects did not occur. Enabling more than one
attempt requires the application callback to make side effects idempotent or deduplicate them.
JobGuardian does not guarantee exactly-once side effects. The retry limit bounds callback
invocations, not wall-clock duration; callbacks must cooperate with cancellation and should enforce
their own operation timeouts.

## 7. Execution history

History is an operational record, not a transactional guarantee for job execution. Failure to
create or update history must be logged and reported through available diagnostics but must not
prevent a callback from running or change the callback's outcome. History may therefore contain
incomplete rows after process failure or persistence outages.

The history API supports:

- Lookup by execution ID
- Query by tenant-scoped job identity
- Required inclusive `started_from_utc` and exclusive `started_before_utc` bounds
- Optional outcome filter
- One-based page number and page size from 1 through 100
- Deterministic ordering by start time descending, then execution ID descending

Retention and deletion are owned by the consumer/operator. The SDK does not delete history
automatically and does not promise a default retention period. Operators must establish and verify
a retention policy appropriate to their operational and regulatory requirements. History can
contain identifiers and failure context and must be protected accordingly.

The PostgreSQL provider does not apply schema migrations automatically. Apply the initial schema
and subsequent versioned migration scripts before upgrading the SDK. History schema version 2.0
adds lease-acquisition tracking, an outcome reason, and query/recovery indexes.

## 8. Correlation and observability

The host may supply a correlation ID for each execution. If none is available, the SDK generates a
GUID. The selected ID remains stable across all callback attempts and is persisted to execution
history when history storage is available. Conversation IDs are optional and are not required by
the .NET 1.0 runtime.

Trace context is separate from correlation ID and follows the OpenTelemetry `Activity` context.
JobGuardian provides ActivitySource and Meter signals; the hosting application configures
listeners, processors, exporters, credentials, and endpoints. The SDK does not open telemetry
endpoints or send telemetry without host configuration.

Metric attributes must be low-cardinality and non-sensitive. Tenant IDs, job IDs/names,
execution IDs, correlation IDs, and error messages must not be metric attributes. Trace and log
attributes may carry execution identifiers where operationally useful, subject to the host's
privacy and retention policy. Exporters and endpoints are host responsibilities.

## 9. Versioning, security, and support

History records the protocol and persistence schema version used for the execution. PostgreSQL
schema scripts are versioned and must be applied in order. A provider upgrade must not silently
claim compatibility with an unapplied schema migration.

JobGuardian distinguishes build compatibility from framework servicing. It does not promise
security or maintenance fixes for a target framework after Microsoft's end-of-support date.
`net8.0` continues to be compiled and tested for now, including after Microsoft's support ends on
2026-11-10. This build compatibility does not extend Microsoft's servicing or create a JobGuardian
security-fix commitment. `net10.0` is currently supported by Microsoft through 2028-11-14.

The lifecycle dates reflect Microsoft's policy as of 2026-10-27 and must be rechecked before each
release. Removing `net8.0` from builds and tests is a separate product decision and must be discussed
before the target is removed. Consumers should move to a Microsoft-supported runtime to receive
Microsoft security and maintenance updates.

## 10. Conformance

An SDK may claim conformance only for behaviors it implements and has verified against the
contract tests in [Contract Tests v1](../contracts/contract-tests-v1.md). Protocol text alone does
not establish implementation conformance. For the initial 1.0 release, conformance is limited to
the .NET SDK/PostgreSQL implementation.
