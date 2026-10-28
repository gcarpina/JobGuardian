# JobGuardian Contract Tests v1

Version: 1.0

Status: Ratified

## Purpose and applicability

These tests define observable Protocol v1 behavior independent of SDK-specific API names. A
conformance suite must run against each supported persistence provider and SDK implementation.
For the JobGuardian 1.0 release, only .NET 8/.NET 10 with PostgreSQL is declared conformant.
In-memory tests provide single-process behavioral coverage, not distributed-provider conformance.

Each persistence test starts from an isolated database state. Time-boundary tests use a provider
test clock or controlled SQL timestamps; they must not rely on arbitrary sleeps. History writes are
best-effort for runtime execution, but direct history-store operations must return their defined
errors.

## Lease and identity contracts

| ID | Scenario | Required observable result |
|---|---|---|
| CT001 | Acquire a free tenant/job identity | Acquisition succeeds and exactly one active owner exists |
| CT002 | Acquire a job with an unexpired lease | Second acquisition fails without changing ownership |
| CT003 | Renew a lease owned by the current execution before expiry | Renewal succeeds and extends the lease using database time |
| CT004 | Acquire when the previous lease has expired | New owner succeeds; old owner cannot renew |
| CT005 | Ten concurrent acquisitions for one identity | Exactly one owner succeeds |
| CT013 | Same namespace/name in two tenants | Both tenants acquire independently |
| CT015 | Client clocks are skewed | Lease decisions remain determined by database time |
| CT016 | Renew a valid active lease | Renewal succeeds and retains ownership |
| CT017 | Renew after ownership transfer | Renewal fails and does not change the new owner |
| CT018 | `lease_until_utc` equals database time | Lease is expired; acquisition may transfer and renewal is rejected |
| CT019 | `heartbeat_interval >= lease_duration`, or either value is non-positive | Registration/execution validation fails before callback invocation |

## Execution outcome and failure-policy contracts

| ID | Scenario | Required observable result |
|---|---|---|
| CT006 | Callback succeeds | `Succeeded` is terminal, history is completed when available, and lease is released |
| CT007 | All callback attempts throw | `Failed` is terminal and includes failure category/context according to provider policy |
| CT008 | Prior acquired execution has no persisted terminal outcome; a later owner acquires after expiry | Prior history is updated to terminal `Abandoned`; recovery does not claim the process is dead |
| CT009 | `FailurePolicy = Ignore` after a failure | The next attempt remains eligible |
| CT010 | `RequireManualReset` after `Failed`, `LeaseLost`, or `Abandoned` | Subsequent attempt is blocked as `Skipped`/`Blocked` until programmatic reset |
| CT011 | Programmatic reset of blocked state | Job becomes eligible; authentication/audit are outside the SDK contract |
| CT020 | Cancellation during callback or shutdown | `Cancelled`; no further callback retry occurs |
| CT021 | Heartbeat cannot renew/confirm ownership | Callback is cooperatively cancelled; final result is `LeaseLost`; no retry occurs |
| CT022 | Busy lease versus blocked job | Both are `Skipped`, with distinct reasons `LeaseBusy` and `Blocked`; callback is not invoked |
| CT023 | `Cancelled` or `Skipped` under `RequireManualReset` | Neither outcome blocks later execution |

## Retry contracts

| ID | Scenario | Required observable result |
|---|---|---|
| CT024 | Default retry settings | Callback runs once (`max_attempts = 1`) |
| CT025 | Callback throws and attempts remain | Another invocation uses the same execution ID, correlation ID, and lease |
| CT026 | Attempt limit reached | Exactly `max_attempts` callback invocations occur; final outcome is `Failed` |
| CT027 | Retry boundaries | Attempts outside 1–10 or fixed delay outside 0–5 minutes are rejected before execution |
| CT028 | Cancellation or lease loss while a retry delay is pending | No next callback invocation occurs |
| CT029 | Retryable callback has an external side effect | Documentation states callers must provide idempotency/deduplication; exactly-once is not claimed |

## History, recovery, and correlation contracts

| ID | Scenario | Required observable result |
|---|---|---|
| CT030 | Query history for one tenant/job and bounded time range | Only the matching job and `[started_from, started_before)` entries are returned |
| CT031 | Query by outcome and page | Filter is applied; page size 1–100 and one-based page are enforced; ordering is deterministic |
| CT032 | Query with invalid job identity, empty/reversed time range, or invalid paging | Query is rejected without issuing an unbounded query |
| CT033 | Two tenants use identical job names | History query for one tenant never returns the other tenant's entries |
| CT034 | Host supplies a correlation ID | ID is persisted and remains stable across all callback retries |
| CT035 | Host does not supply a correlation ID | SDK generates one and persists the same ID for the coordinated execution |
| CT036 | A trace parent exists | JobGuardian execution Activity uses the current Activity context as its parent |
| CT037 | History create/update fails | Callback behavior and final outcome are not replaced; the persistence error is logged |
| CT038 | Recovery cannot persist `Abandoned` | New owner proceeds; failure is reported and entry remains eligible for later recovery |
| CT039 | History retention | SDK performs no automatic deletion; consumer/operator owns and documents retention |

## Observability and scope contracts

| ID | Scenario | Required observable result |
|---|---|---|
| CT040 | Host listens to JobGuardian ActivitySource and Meter | Execution traces and documented metrics are observable |
| CT041 | Metric labels are inspected | No tenant/job/execution/correlation IDs or error messages appear as metric attributes |
| CT042 | No telemetry exporter is configured by host | JobGuardian opens no telemetry endpoint and sends no telemetry independently |
| CT043 | OIDC/RBAC, dashboard, manual-run, or force-takeover capability is requested | These are not represented as SDK 1.0 capabilities |
| CT044 | SDK conformance is declared | Claim is backed by an implementation and passing tests; .NET is the only 1.0 conformant SDK |
| CT045 | Reset audit persistence fails | The reset is not applied and the persistence error is propagated |
| CT046 | Reset is accepted, including when already eligible | `ResetJobRequested` is persisted first with tenant, job, actor ID, and UTC timestamp |
| CT047 | Actor ID is empty or whitespace | The request is rejected before audit or state persistence |

## Existing v1 contract scenarios retained

CT001–CT017 from earlier draft revisions are retained where their intent is represented above.
General administrative audit, dashboard actions, and authentication/authorization are outside the
SDK conformance suite. The SDK reset-request audit behavior is covered by CT045–CT047.

## Conformance result

An implementation conforms only when all applicable tests pass for each advertised target
framework and provider. A test marked not applicable must have an explicit protocol boundary
rationale; it must not be silently omitted from a claimed conformance report.
