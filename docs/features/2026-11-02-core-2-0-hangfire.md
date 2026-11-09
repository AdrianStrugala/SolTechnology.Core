---
status: completed
created: 2026-11-02
completed: 2026-11-09
---

# SolTechnology.Core 2.0 — Hangfire

> Historical delivery record. It may not describe the current system.

## Goal

Deliver the 2.0 hardening of `SolTechnology.Core.Hangfire` end-to-end, as one library of the SolTechnology.Core 2.0
flagship release. Success means:

- `preventOverlap` works through `PreventOverlapJobFilter`, and the correlation service is registered (`B5`, `infra#13`, A7, RM-10).
- Ambient correlation state is restored after each job, and event dispatch is async and cancellable while 1.x event jobs keep running (P-05, RQ10, RM-6, RM-7).
- `IJob.Execute` returns `Task<Result>`, recurring job IDs use a version-free full name, and legacy `Type.Name` IDs are removed automatically (`C-19`, P-06, A6, RM-3, RM-8, RM-9).
- Hangfire ships XML docs, a `docs/Hangfire.md` README that compiles against 2.0, and conforms to the Coding Guide without changing persisted job identity (RM-7, RM-16).

## Context

**Depends on:**

- Core 02, 03, and 04 of [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) are `done` (CI fails on any test failure, tag-only publishing, version 2.0.0 on `master`).
- [`2026-11-02-core-2-0-core-result`](2026-11-02-core-2-0-core-result.md) is `completed`.
- [`2026-11-02-core-2-0-logging`](2026-11-02-core-2-0-logging.md) is `completed`.
- [`2026-11-02-core-2-0-cqrs`](2026-11-02-core-2-0-cqrs.md) is `completed`.

Delivery order position 12 of 13 in the umbrella brief's "Per-library regroup" section. The
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md) feature
depends on this feature being `completed`.

### Origin

Regrouped on 2026-11-02 from [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) (user: "podziel implementację feature 1 na
każdą bibliotekę osobno"). Each step names its former core step in a "Former core step" line; the
umbrella brief's "Per-library regroup" table maps every former number. The evidence is the
full-repository code review of 2026-11-02 against `master@4097d47f`; finding IDs follow the
umbrella brief's "Evidence" table. Findings acted on here: `B5`, `P1-2`, `P1-4`, `cqrs-tale#16`, `infra#8`, `infra#9`, `infra#10`, `infra#13`, `infra#18-19`, `infra#23`, `infra#24`.

### Decisions owned by this feature

The umbrella brief keeps the shared decision tables and the user's confirmations (`C-01`…`C-24`,
`C-26`, `C-27` confirmed, `C-25` rejected, 2026-11-02). The rows below are copied from it. Step
numbers in copied `A`, `AR`, `Q`, and `RQ` rows are former core numbers.

Public API changes (`CLAUDE.md §2`, confirmed by the user on 2026-11-02):

| ID | Package | Exact change | Step |
|---|---|---|---|
| C-19 | Hangfire | `IJob.Execute(CancellationToken)` returns `Task<Result>` instead of `Task`; `AddSolRecurringJob<TJob>` gains optional `string? jobId = null, TimeZoneInfo? timeZone = null` (binary break) | hangfire 04 |
| C-32 | Hangfire (additive, user, 2026-11-09) | NEW `RecurringJobsOptions` with `bool RemoveStaleRecurringJobs` (default `false`), set through `services.Configure<RecurringJobsOptions>(...)`; turns on the removal of recurring jobs this application no longer registers | hangfire code review |

Behavioural and persisted-contract breaks:

| ID | Change | Consumer impact | Step |
|---|---|---|---|
| P-05 | New event jobs target `Task DispatchInScope(IEvent, CancellationToken)` (internal type, persisted job signature); the 1.x `DispatchInScope(IEvent)` overload stays for 2.x (RQ10) | Already-enqueued 1.x event jobs still run; the legacy overload is removed in 3.0 | hangfire 03 |
| P-06 | Recurring job id `Type.Name` → version-free full type name (namespace, name and generic arguments, no assembly information; RM-3), or, when that is longer than 86 characters (Hangfire.SqlServer keeps `recurring-job:<id>` in an `nvarchar(100)` key), `Type.Name` truncated as needed + `-` + the first 12 hex characters of the full name's SHA-256; overridable with `AddSolRecurringJob(..., jobId:)`, where an id over 86 characters throws `ArgumentException`; the cron time zone is `timeZone ?? UTC`. At start the registrar always removes each job's legacy `Type.Name` entry when that entry runs the same job and is not a current id (RQ10). Removing ids it no longer registers is opt-in (`RecurringJobsOptions.RemoveStaleRecurringJobs`, default `false`, `C-32`; code review 2026-11-09): with it on, the registrar removes the ids it recorded in this application's set `sol:recurring-ids:<IHostEnvironment.ApplicationName>` that the starting instance no longer registers (RM-8) | 1.x recurring entries are removed automatically and re-created under new ids; no double firing; a legacy entry of another job type with the same `Type.Name` is kept. A job you stop registering, rename or move stays in storage unless you delete it in the dashboard or turn the option on; with the option on, every instance must register the same jobs. After a rollback to 1.x, delete the 2.x ids from the dashboard | hangfire 04, hangfire code review |
| P-25 | `PreventOverlapJobFilter` holds a non-blocking distributed storage lock for the run (`sol:overlap:<recurring job id>`, or a hash of the method and arguments outside recurring jobs) instead of scanning every scheduled and processing job (premortem RM-10); `AddSolRecurringJob(..., preventOverlap: true)` now applies it through a filter provider that `UseSolFilters` installs (B5, A7) | A run is cancelled only while another matching run is processing: a run waiting in `Scheduled` for a retry no longer blocks the next trigger. Each guarded run holds one storage lock for its duration (on SQL Server storage, one dedicated connection, as with `DisableConcurrentExecution`). Without `UseSolFilters` the registrar logs a Warning at start and overlap is not prevented | hangfire 02 |
| P-26 | CQRS `EventDispatcher`: an `OperationCanceledException` that an event handler throws while the dispatch's `CancellationToken` is cancelled stops the dispatch; the remaining handlers are not called and the exception propagates from `IEventDispatcher.Dispatch` instead of being logged as an Error and swallowed per handler. Other handler exceptions, including an `OperationCanceledException` with the token not cancelled, are still logged and swallowed (code review 2026-11-09) | A custom `IEventPublisher` that dispatches with a cancellable token must expect `OperationCanceledException`; the in-memory publisher passes `CancellationToken.None` and is unaffected. Hangfire event jobs interrupted by shutdown are requeued, and a dispatch whose handlers all finished before shutdown succeeds | hangfire code review (cqrs) |
| P-27 | `ICorrelationIdService` (Logging) gains `void Clear()`, which removes the ambient correlation id; the built-in implementation clears its `AsyncLocal`, and Hangfire's correlation filter uses it to restore "no id" after a job (code review 2026-11-09, user decision) | A custom `ICorrelationIdService` implementation must add `Clear()`: it no longer compiles, and one compiled against 1.x fails to load (`TypeLoadException`) | hangfire code review (logging) |

Design alternatives (blue-red-team, 2026-11-02):

| # | Choice | Alternatives | Recommendation and crux |
|---|---|---|---|
| A6 | Hangfire Smart Retry for plugin jobs (`infra#10`) | (a) `IJob.Execute` returns `Task<Result>` so `SmartRetryJobFilter` sees a `Result`; (b) keep `Task` and remove the claim from docs | **(a)** if `C-19` is confirmed, otherwise (b). Aligns jobs with Guide §13. |
| A7 | `preventOverlap` (`B5`) | (a) implement through `PreventOverlapJobFilter` (already in `Hangfire/Filters/`); (b) delete the parameter | **(a).** Non-breaking, and the docs already promise it. |

User decisions (2026-11-02):

| ID | Question | Decision (user, 2026-11-02) | Steps |
|---|---|---|---|
| RQ10 | Hangfire persisted contracts | **(a)** Keep the legacy one-argument `DispatchInScope(IEvent)` for 2.x and remove legacy `Type.Name` recurring IDs automatically, with tests. | 32, 51 |

Premortem mitigations carried verbatim from core 00: RM-10 (step 02), RM-6 and RM-7 (step 03), RM-3, RM-7, RM-8, RM-9 (step 04), RM-6, RM-8, RM-9 (step 06), RM-7 and RM-16 (step 07).

Release-notes input owned here: the core brief's `hangfire 03` entry (1.x event jobs keep running in 2.x; the legacy one-argument overload is removed in 3.0). Hangfire.Tests gains `Hangfire.InMemory 1.0.0` (step 01). Hangfire has no L-03 template row; step 07 runs the library-scoped `[{Placeholder}]` check.

## Scope

### In scope

- Source: `src/SolTechnology.Core.Hangfire`, and the matching test projects.
- Module docs: `docs/Hangfire.md`.
- Phases, in order: correctness and broken-module fixes → 2.0 breaking API cleanup → XML docs and
  module docs → Coding Guide conformance and `[{Placeholder}]` log templates → retrospective.

### Out of scope

- Removing the legacy `DispatchInScope(IEvent)` overload (3.0).
- Work owned by the foundation feature (CI, publishing, version, DreamTravel exception page, secret
  artifacts, zero-dependency base package, central `GenerateDocumentationFile`), the other library
  features, the showcase feature, and the release feature.

## Implementation plan

Steps lived in the temporary working folder `2026-11-02-core-2-0-hangfire`, which the retrospective deleted on
2026-11-09. Gates: `review` was `done (2026-11-02)`; `premortem` was `skipped` — the core premortem verdict
`go-with-mitigations (2026-11-02)` covered these steps.

Plan review (2026-11-02): the regrouping was checked against the pre-regroup plan (`dddac21c`) and
found complete. User answers recorded, not re-asked: the delivery order with AUID before Tale is
confirmed (Tale references AUID); the per-library split, the premortem reuse, and the `C-01`…`C-24`,
`C-26`, `C-27` confirmations stand (`C-25` rejected). Review fixes applied here: the retrospective's DreamTravel test command is runnable.

Notation: an unqualified step number is a step of this feature; `core NN` names step `NN` of
[`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md); other short names (`result`, `logging`, `cqrs`, `auid`, `tale`, `http`, `api-auth`,
`cache`, `sql`, `blob`, `messagebus`, `hangfire`, `testing-kits`) name the library features listed
in the umbrella brief; `release NN` names a step of
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md).

| Step | Title | Former core step(s) |
|---|---|---|
| 01 | DI resolution test — Hangfire | 11 |
| 02 | Hangfire — implement `preventOverlap`; register the correlation service | 13 |
| 03 | Hangfire — restore ambient correlation state; async, cancellable event dispatch | 32 |
| 04 | Hangfire — recurring job contract for 2.0 | 51 |
| 05 | XML docs shipped — Hangfire | 57 |
| 06 | Module docs — `docs/Hangfire.md` | 61 |
| 07 | Guide conformance — Hangfire | 69, 70 (library-scoped check) |
| 08 | Retrospective | 71 |

Steps run in number order; each is one PR.

## Acceptance criteria

- [x] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations`.
- [x] Regression tests for `B5` and `infra#13` (Hangfire) fail on `4097d47f` and pass; the Hangfire resolution test has no `[Ignore]`.
- [x] `C-19` shipped; `HangfirePersistedIdentityTests` is green and unchanged after step 07 (RM-7, RM-16).
- [x] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green.
- [x] The module resolution test uses `ValidateOnBuild = true` and `ValidateScopes = true`, and no resolution test is `[Ignore]`d.
- [x] `find src/SolTechnology.Core.Hangfire -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] Every packed `.nupkg` of this feature contains `lib/net10.0/*.xml`.
- [x] Every C# snippet in `docs/Hangfire.md` compiles in a throwaway console project against the 2.0 API, as recorded in the module-docs step.
- [x] The library-scoped `[{Placeholder}]` template check of the conformance or log-template step prints nothing.

## Completion summary

Delivered on `master` from `fca12b5d` (step 01) to `32e39c89` (step 07), plus the step 03 follow-up fix `6608121e`;
the code review fixes (`7a7af4f5` to `07fac5b7`) and this retrospective ran on 2026-11-09. The owner pre-approved the
plan and asked on 2026-11-09 to run the retrospective and close the feature (`CLAUDE.md §2`).

- **`infra#13` (steps 01, 02):** both installers register `ICorrelationIdService`; `ModuleResolutionTests` resolves
  every Hangfire service with `ValidateOnBuild` and `ValidateScopes` over in-memory storage, without `[Ignore]`.
- **`B5`, A7, RM-10, P-25 (step 02):** `preventOverlap` works through `PreventOverlapFilterProvider`, which
  `UseSolFilters` installs; `PreventOverlapJobFilter` holds a non-blocking storage lock instead of scanning every job.
- **`infra#8`, `infra#9`, P-05, RM-6, RM-7 (step 03 and code review 3, 4):** the correlation filter no longer writes
  ambient state on create and restores (or clears, through the new `ICorrelationIdService.Clear()`, P-27) the worker's
  id after a job. Event jobs target the async `DispatchInScope(IEvent, CancellationToken)`; the 1.x overload stays.
  CQRS's dispatcher propagates a cancellation of its token (P-26), so an event interrupted by shutdown is requeued and a
  finished one is not. `HangfirePersistedIdentityTests` pins the persisted job identity.
- **`C-19`, A6, P-06, RM-3, RM-8, RM-9 (step 04 and code review 1, 2, 5):** `IJob.Execute` returns `Task<Result>`; a
  recoverable failed `Result` is retried at most 3 times, a throw never. Ids are the version-free full type name,
  shortened to at most 86 characters for Hangfire.SqlServer, or `jobId`; `timeZone` defaults to UTC. 1.x `Type.Name`
  entries are removed at start; removal of jobs the application no longer registers is opt-in
  (`RecurringJobsOptions`, `C-32`). Start-up warnings 2150 and 2151 report a missing `UseSolFilters`.
- **Docs (steps 05, 06, code review 6):** the package ships XML docs; `docs/Hangfire.md` was rewritten for 2.0 with
  every C# block compiled, including the Start-up logs table, the 1.x upgrade and rollback rows, and the job id rules.
- **Conformance (step 07):** `ModuleInstaller` uses `extension` blocks; every file is under 150 lines; every log
  template brackets its placeholders. ApiCompat shows only the compiler-generated `extension` marker types.
- **Architecture:** [`docs/architecture/background-processing.md`](../architecture/background-processing.md) describes
  the 2.0 behaviour: event dispatch and shutdown, recurring job ids and cleanup, retries (A6), filters including the
  overlap guard (A7), start-up warnings, and the pinned persisted identity.
  [`cqrs.md`](../architecture/cqrs.md) gained the cancellation rule in the code review;
  [`core-result-and-errors.md`](../architecture/core-result-and-errors.md) notes that a recurring job's `Result` is
  only written into the `Succeeded` state. The premortem reference `.github/skills/premortem/references/hangfire.md`
  gained the id-length, cleanup-scope, shutdown-abort, and process-wide-state checks.
- **EventIds:** Hangfire took 2150–2153, which the Chronicle plan had reserved; `6c120357` moved Chronicle to 2154 and
  2155 in its brief and steps. The repository has no central EventId range table; module docs list their own ids.
- **Final verification (2026-11-09):** `dotnet build SolTechnology.Core.slnx -c Release` reported 0 errors and no
  NU190x / NU1605; `dotnet test tests/SolTechnology.Core.Hangfire.Tests` 73/73; the DreamTravel component and unit tests
  green; the Release pack contains `lib/net10.0/SolTechnology.Core.Hangfire.xml`. The last full suite, on the code of
  this record (`07fac5b7`; the retrospective changed only documentation), passed 917, skipped 11 (DreamTravel
  geolocation), failed 0.

Changes to the core brief ([`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md)) for
release 05: new rows `C-32`, `P-25`, `P-26`, `P-27`; P-06 rewritten twice (id shortening, opt-in stale removal);
`C-19` and P-05 shipped as written. "Release-notes inputs" gained the RM-6 deploy-order note, the RM-8 rollback note,
the `C-19`/RM-9 retry note, and the code review entries (id length, opt-in cleanup, CQRS cancellation, `Clear()`,
start-up logs, last-job and rollback drain). No L-row: Hangfire had no log template before 2.0.

## Deviations

### Resolution tests and test packages (step 01)

**Original plan:** one resolution test ignored for infra#13; the test project gains `Hangfire.InMemory`.

**Actual implementation:** two tests cite infra#13, because the strict-validation test fails at `BuildServiceProvider`
before it can show the missing service; the test project also references `Hangfire.NetCore` (for `AddHangfire`) and,
since code review 1, `Hangfire.SqlServer` and `SolTechnology.Core.SQL.Testing`.

### Overlap lock key and semantics, P-25 (step 02)

**Original plan:** replace the monitoring scans, for example with a lock named `sol:overlap:<recurring job id>`.

**Actual implementation:** the lock resource is `sol:overlap:` plus the `RecurringJobId` job parameter, or a SHA-256 of
the invocation for jobs that use the filter as an attribute. A run waiting in `Scheduled` for a retry no longer blocks
the next trigger (P-25). The provider lives in the process-wide `JobFilterProviders.Providers` and replaces one an
earlier host added; tests reset Hangfire's static state through the test helper `HangfireGlobalState`.

### Correlation restore and event dispatch (step 03, code review 3, 4)

**Original plan:** restore the previous ambient id; the legacy overload forwards with `CancellationToken.None`.

**Actual implementation:** "no previous id" is restored through the new `ICorrelationIdService.Clear()` (P-27; first
through `Set(null)`); the legacy overload returns `Task`. The step 03 follow-up threw after every dispatch that saw a
cancelled token; the code review replaced it with the CQRS dispatcher change (P-26), because the throw also requeued
finished work. The RM-6 release note also names Hangfire's global retry filter as the source of 1.x-server retries.

### Retry budget, legacy-id guard and cleanup (step 04, code review 1, 2, 5)

**Original plan:** an explicit bounded retry count; remove legacy `Type.Name` ids and stale tracked ids at start.

**Actual implementation:** the budget is 3 through `[AutomaticRetry(OnlyOn = RecoverableResultException)]`; Smart Retry
elects that internal exception type (an `InvalidOperationException`). A legacy entry is removed only when it runs the
same runner. Long default ids are shortened (Hangfire.SqlServer column limit), stale removal is opt-in (`C-32`), and
the start-up logs use EventIds 2150–2153 through `RecurringJobLogMessages`; `RecurringJobCleanup` is a DI singleton.

### Module page rewrite (step 06)

**Original plan:** edit the listed lines of `docs/Hangfire.md`.

**Actual implementation:** the page was rewritten to the canonical module-page structure; rate limiting, worker count,
database migration and the delay-queue note became Conventions bullets.

### Conformance scope (step 07)

**Original plan:** `extension` blocks, braces and comments across the library.

**Actual implementation:** steps 02–05 already met the Guide, so only `ModuleInstaller.cs` changed.

## Follow-ups

- Remove the legacy one-argument `HangfireEventPublisher.DispatchInScope(IEvent)` in 3.0 (step 03, RQ10).
- A recurring job's non-recoverable failed `Result` ends `Succeeded` without a log entry (Smart Retry's existing
  semantics); consider logging it or electing `Failed`.
- Testing kits: the Hangfire tests carry their own harness in the test project — `RecurringJobHost` (registrar start
  per application name on a shared storage), the performer and state-machine loop in `RecurringJobRetryTests`, and
  `HangfireGlobalState.Reset`, which undoes the static log provider, `GlobalJobFilters` and
  `JobFilterProviders.Providers` writes of `AddHangfire`/`UseSolFilters`. Any shipped Hangfire testing helper needs that
  reset, or a disposed test container breaks later `InMemoryStorage` construction. The SQL Server test uses
  `SQLFixture("SolHangfireTests")` (container `sol-sql-solhangfiretests`, about 20 s on arm64). Owner: the
  testing-kits feature.
- Chronicle step 14 (`2026-11-02-logging-chronicle`): `PreventOverlapJobFilter` is method-scoped and runs after global
  filters, so a global Chronicle filter starts its root before the overlap guard cancels a run (Hangfire then records
  `Deleted`); a recurring job's failed `Result` is a return value, not an exception, so the planned outcome mapping
  reports it as `Ok` unless it reads `PerformedContext.Result`.
- Pre-existing build warnings seen in the final Release build, outside this feature: CA2022 in
  `tests/SolTechnology.Core.Logging.Tests/Middleware/LoggingMiddlewareBodyBufferingTests.cs` and CS0105 in the Tale tests.

## Code review fixes (2026-11-09)

An independent code review of `c91b4820..32e39c89` found six issues; each was fixed with the user's decision, in its
own commit. Rows: `C-32`, P-06 (amended), `P-26`, `P-27`; release-notes inputs are in the core brief's
"Release-notes inputs" section. Step 08 (retrospective) has not run.

| # | Finding | Fix | Commit | Test |
|---|---|---|---|---|
| 1 | High. Hangfire.SqlServer keeps `recurring-job:<id>` in an `nvarchar(100)` key, so a default id over 86 characters (the 96-character generic test job) stopped the host with "String or binary data would be truncated". | A default id that does not fit is `Type.Name` (truncated to 73) + `-` + 12 hex characters of the full name's SHA-256; an explicit `jobId` over 86 characters throws `ArgumentException` naming the limit; the tracked-set key is bounded to 100 the same way. P-06 amended; `Hangfire.md` states the limit. | `7a7af4f5` | `RecurringJobSqlServerTests.AddSolRecurringJob_LongGenericTypeName_RegistersOnSqlServer` (SQL Server through `SQLFixture`) fails with the truncation error without the fix; `StorageKeyTests`, `AddSolRecurringJob_JobIdLongerThan86_ThrowsNamingTheLimit`, `Registrar_Legacy1xIdOfLongGenericJob_IsRemovedOnStart`. |
| 2 | Medium. Stale-id removal deleted the jobs that other instances of the same application still registered (feature flags), and two applications sharing a job type from one package deleted each other's entry. | Opt-in `RecurringJobsOptions.RemoveStaleRecurringJobs` (default `false`, `C-32`) through `services.Configure<RecurringJobsOptions>`; 1.x legacy removal stays always on. `Hangfire.md` describes both cases. | `cb2ca18a` | `Registrar_RemovedJobWithoutOptIn_IsKept`; the removal and shared-storage tests run with the option on. |
| 3 | Medium. The unconditional `ThrowIfCancellationRequested` after dispatch requeued event jobs whose handlers had all finished, so every deploy re-ran them. | Throw removed. CQRS's `EventDispatcher` rethrows an `OperationCanceledException` when the dispatch's token is cancelled and calls no further handler; other exceptions are still logged and swallowed (`P-26`). `architecture/cqrs.md`, `IEventDispatcher` and `IEventHandler` docs updated. | `8c552819` | `EventDispatcherCancellationTests` (CQRS, 3 tests) and `HangfireEventShutdownTests` (finished dispatch succeeds; interrupted dispatch throws `OperationCanceledException` so Hangfire requeues, and the next handler is not called). |
| 4 | Low. `CorrelationIdJobFilter` passed `null` to the non-nullable `ICorrelationIdService.Set`. | `ICorrelationIdService` (Logging) gains `Clear()` (`P-27`); the built-in service and three test fakes implement it; the filter calls it. `Log.md` lists it. | `87d0939b` | `CorrelationIdServiceTests.Clear_RemovesAmbientCorrelation`, `CorrelationIdJobFilterTests.OnPerformed_NoPreviousAmbientId_ClearsIt`. |
| 5 | Low. Smart Retry was silently off without `UseSolFilters`. | The registrar warns at start (EventId 2151) when recurring jobs are registered and `GlobalJobFilters` holds no `SmartRetryJobFilter`; the module's four start-up logs use source-generated messages with EventIds 2150–2153; `Hangfire.md` has a Start-up logs table. No L-row: the logs are new in 2.0. | `e8fc68bf` | `Registrar_WithoutSmartRetryFilter_LogsWarning`, `Registrar_WithSmartRetryFilter_LogsNoSmartRetryWarning`; the overlap warning test also checks EventId 2150. |
| 6 | Low. Docs did not say that removing the last `AddSolRecurringJob` call leaves old entries, or that 2.x event jobs must be drained before a rollback to 1.x. | `Hangfire.md` (Job ids and Upgrading tables) and the release notes cover both. | `222be430` | — |

Verification after the fixes: `dotnet build SolTechnology.Core.slnx -c Release` reports 0 errors and no NU190x/NU1605;
its warnings are pre-existing (CA2022 in `LoggingMiddlewareBodyBufferingTests`, CS0105 in Tale tests, the DreamTravel
sample). All seven C# blocks of `Hangfire.md` compile in a throwaway project. `dotnet test SolTechnology.Core.slnx
-c Release`: 917 passed, 11 skipped (DreamTravel geolocation), 0 failed — the 901 baseline plus 12 Hangfire, 3 CQRS
and 1 Logging tests.

