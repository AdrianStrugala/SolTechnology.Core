---
status: in-progress
created: 2026-11-02
completed:
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

Behavioural and persisted-contract breaks:

| ID | Change | Consumer impact | Step |
|---|---|---|---|
| P-05 | New event jobs target `Task DispatchInScope(IEvent, CancellationToken)` (internal type, persisted job signature); the 1.x `DispatchInScope(IEvent)` overload stays for 2.x (RQ10) | Already-enqueued 1.x event jobs still run; the legacy overload is removed in 3.0 | hangfire 03 |
| P-06 | Recurring job id `Type.Name` → version-free full type name (namespace, name and generic arguments, no assembly information; RM-3), or, when that is longer than 86 characters (Hangfire.SqlServer keeps `recurring-job:<id>` in an `nvarchar(100)` key), `Type.Name` truncated as needed + `-` + the first 12 hex characters of the full name's SHA-256; overridable with `AddSolRecurringJob(..., jobId:)`, where an id over 86 characters throws `ArgumentException`; the cron time zone is `timeZone ?? UTC`. At start the registrar removes the ids it recorded in this application's set `sol:recurring-ids:<IHostEnvironment.ApplicationName>` that are no longer registered (RM-8), and each job's legacy `Type.Name` entry when that entry runs the same job and is not a current id (RQ10) | Old recurring entries are removed automatically and re-created under new ids; no double firing. Applications that share one storage keep each other's jobs; a legacy entry of another job type with the same `Type.Name` is kept. After a rollback to 1.x, delete the 2.x ids from the dashboard | hangfire 04 |
| P-25 | `PreventOverlapJobFilter` holds a non-blocking distributed storage lock for the run (`sol:overlap:<recurring job id>`, or a hash of the method and arguments outside recurring jobs) instead of scanning every scheduled and processing job (premortem RM-10); `AddSolRecurringJob(..., preventOverlap: true)` now applies it through a filter provider that `UseSolFilters` installs (B5, A7) | A run is cancelled only while another matching run is processing: a run waiting in `Scheduled` for a retry no longer blocks the next trigger. Each guarded run holds one storage lock for its duration (on SQL Server storage, one dedicated connection, as with `DisableConcurrentExecution`). Without `UseSolFilters` the registrar logs a Warning at start and overlap is not prevented | hangfire 02 |

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

Steps live in the temporary working folder `2026-11-02-core-2-0-hangfire/` (see its `summary.md`). Gates: `review` is
`done (2026-11-02)`; `premortem` is `skipped` — the core premortem verdict `go-with-mitigations (2026-11-02)`
covers these steps.

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

- [ ] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations`.
- [ ] Regression tests for `B5` and `infra#13` (Hangfire) fail on `4097d47f` and pass; the Hangfire resolution test has no `[Ignore]`.
- [ ] `C-19` shipped; `HangfirePersistedIdentityTests` is green and unchanged after step 07 (RM-7, RM-16).
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green.
- [ ] The module resolution test uses `ValidateOnBuild = true` and `ValidateScopes = true`, and no resolution test is `[Ignore]`d.
- [ ] `find src/SolTechnology.Core.Hangfire -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] Every packed `.nupkg` of this feature contains `lib/net10.0/*.xml`.
- [ ] Every C# snippet in `docs/Hangfire.md` compiles in a throwaway console project against the 2.0 API, as recorded in the module-docs step.
- [ ] The library-scoped `[{Placeholder}]` template check of the conformance or log-template step prints nothing.

## Completion summary

## Deviations

Decisions taken during implementation (owner pre-approved the plan; detail in each step's `Deviations` until the
retrospective promotes them):

- Step 01: two resolution tests cite infra#13, and the test project also references `Hangfire.NetCore` for
  `AddHangfire`.
- Step 02: the overlap lock is keyed by the `RecurringJobId` job parameter, or a hash of the invocation outside
  recurring jobs; runs waiting in `Scheduled` for a retry no longer block the next trigger (P-25). The filter provider
  lives in Hangfire's process-wide `JobFilterProviders.Providers` and replaces one installed by an earlier host.
- Step 03: `OnPerformed` restores an absent ambient correlation id with `ICorrelationIdService.Set(null)` (no public
  `Clear()` exists); the legacy `DispatchInScope(IEvent)` returns `Task` instead of `void`; the RM-6 release note also
  says that the 1.x-server retries come from Hangfire's global retry filter.
- Step 03 (follow-up fix): an event job whose dispatch saw the shutdown token cancelled throws
  `OperationCanceledException` after dispatch, because CQRS's dispatcher swallows the handler's cancellation; Hangfire
  then requeues the event instead of marking it `Succeeded`.
- Step 04: a recoverable failed `Result` is retried at most 3 times (`RecurringJobRetry.RecoverableResultAttempts`)
  through `[AutomaticRetry(OnlyOn = RecoverableResultException)]` on the runner; a thrown exception is not retried.
  Legacy `Type.Name` entries are removed only when they run the same job's runner. Tracked-id and legacy cleanup live in
  `RecurringJobCleanup` and log each removal at Information.
- Step 06: `docs/Hangfire.md` is rewritten to the canonical module-page structure; the rate-limiting, worker-count,
  database-migration and delay-queue sections shrink to Conventions bullets. All six snippets compile in a throwaway
  project.
- Code review 1 (job id length): a default id whose full type name exceeds 86 characters becomes `Type.Name`
  (truncated to 73) + `-` + 12 hex characters of the full name's SHA-256 (internal `StorageKey`); an explicit `jobId`
  over 86 characters throws `ArgumentException`. The per-application tracked-set key is bounded to 100 characters the
  same way. Chosen test: `RecurringJobSqlServerTests` registers the 96-character generic job against SQL Server
  (`SQLFixture`, about 20 s) and fails with "String or binary data would be truncated" without the fix; unit tests pin
  the rule. The Hangfire test project therefore references `Hangfire.SqlServer` and `SolTechnology.Core.SQL.Testing`.
- Step 07: only `ModuleInstaller` changed (extension blocks and one private filter-registration member); ApiCompat
  shows only the compiler-generated `extension` marker types.

## Follow-ups
