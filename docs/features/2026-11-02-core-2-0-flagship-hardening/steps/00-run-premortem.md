---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 00
status: done
---

# Step 00: Run premortem (gate)

## Summary
A premortem is mandatory (`CLAUDE.md §4`). The plan edits `ModuleInstaller.cs` files,
`src/Directory.Build.props`, and persisted contracts (listed below). Run the
[premortem skill](../../../../.github/skills/premortem/SKILL.md) in a session that did not author
this plan, after `review` is `done`. Record the full output here and the verdict in `summary.md`
`premortem:`. This step changes no `src/` code. Scope: steps 01–70 of this feature only. The
feature was split on 2026-11-02 (RQ8, user); the showcase hygiene and the org transfer and 2.0
release run their own premortems in
[`2026-11-02-flagship-showcase-hygiene`](../../2026-11-02-flagship-showcase-hygiene.md) and
[`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md).

## Affected components
- `docs/features/2026-11-02-core-2-0-flagship-hardening/steps/00-run-premortem.md` — EDIT — append a `## Premortem` section (before `## Deviations`) with the full skill output
- `docs/features/2026-11-02-core-2-0-flagship-hardening/summary.md` — EDIT — `premortem:` verdict
- `.github/skills/premortem/references/hangfire.md`, `auid.md`, `api.md`, `authentication.md`, `testing-kits.md` — NEW — missing module checklists
- Step files named by required mitigations — EDIT (by the executing `implement-plan` session)

## Changes

- **Modules touched:**
  - Libraries:
    - `SolTechnology.Core`, `.CQRS`, `.Tale`, `.AUID`, `.Logging`, `.HTTP`, `.API`,
      `.Authentication`.
    - `.Cache`, `.MessageBus`, `.SQL`, `.Blob`, `.Hangfire`.
  - Testing kits:
    - `.Testing`, `.HTTP.Testing`, `.API.Testing`.
    - `.SQL.Testing`, `.Redis.Testing`, `.Blob.Testing`, `.ServiceBus.Testing`.
  - Kept for reference only, unchanged except a `GenerateDocumentationFile` opt-out (Q1, RQ4): `.Guards`, `.Scheduler`.
  - Other: DreamTravel (steps 01, 08, 09, 15, 17, 29, 36, 38, 39, 40, 41, 43, 44, 45, 46, 49, 50, 51, 53), CI runner and workflow (steps 02, 03), docs.
- **Contract delta:** see the feature brief
  [`../../2026-11-02-core-2-0-flagship-hardening.md`](../../2026-11-02-core-2-0-flagship-hardening.md):
  - "Public API changes requiring §2 confirmation" (`C-01`…`C-24`, `C-26`, `C-27` confirmed 2026-11-02; `C-25` rejected).
  - "Behavioural and persisted-contract breaks" (`P-01`…`P-17`).
  - "Log contract changes" (`L-01`…`L-05`).
  - Leaf package override inside shipped packages: `System.Security.Cryptography.Pkcs 10.0.13` in SQL and SQL.Testing (step 01, user-confirmed RQ1).
  - Semver: MAJOR (2.0.0).
- **`ModuleInstaller.cs` edits:**
  - Library installers: steps 12, 13, 14, 21, 22, 26, 28, 29, 33, 34, 43, 51, 52, 63, 64, 65, 66, 67, 68, 69, 70 (HTTP `:143` log template).
  - XML doc comments only (no code change): steps 55 (CQRS, Tale), 56 (HTTP, Api, Authentication), 57 (Cache, MessageBus, SQL, Blob, Hangfire); their `**/*.cs` globs include each module's `ModuleInstaller.cs`.
  - DreamTravel installers: step 43 (`DreamTravel.Workflows/ModuleInstaller.cs`).
- **`Directory.Build.props` edits:**
  - `src/Directory.Build.props`: steps 04 (version), 54 (shared package references), 58 (`GenerateDocumentationFile`).
  - Authorship and repository URLs in both `Directory.Build.props` files moved to release step 02 (split, 2026-11-02).
- **Persisted contracts:**
  - Step 17 (`Error` JSON; `ChapterInfo.Error` content now written with `TaleJsonOptions.Default` by `TaleInstance.Clone()` and the DreamTravel SQLite repository).
  - Step 18 (what gets persisted).
  - Step 19 (`Auid.TryParse` rejects out-of-range IDs that 1.x stored).
  - Step 27 (`ChapterInfo.Error` content: no stack trace after a deserialization failure).
  - Step 29 (Redis keys).
  - Step 32 (Hangfire event job signature; legacy one-argument overload kept).
  - Step 35 (AUID JSON errors).
  - Step 38 (`Result` JSON with `private init`).
  - Step 41 (`TaleInstance.Version`, DreamTravel SQLite schema).
  - Step 42 (history key).
  - Step 44 (`Auid` type and namespace inside persisted Tale contexts).
  - Step 50 (Blob metadata write path).
  - Step 51 (recurring job IDs, automatic legacy-ID removal, `IJob` result).
- **Applicable checklists** (`.github/skills/premortem/references/`): `cqrs.md`, `story.md` (Tale), `logging.md`, `http.md`, `messagebus.md`, `sql.md`, `blob.md`, `cache.md`, `di.md`, `build-and-nuget.md`. **No checklist exists** for Hangfire, AUID, Api, Authentication, or the testing kits. Per the skill, the premortem writes the missing reference files in the same change.
- **Steps under review:**
  - Phase A (build, CI, version):
    [01](01-clear-nu1903-at-source.md), [02](02-fail-ci-on-any-test-failure.md),
    [03](03-publish-only-from-tags.md), [04](04-bump-version-2-0-0.md).
  - Phase B (security):
    05,
    06,
    07,
    08,
    09.
  - Phase C (DI safety net):
    10,
    11.
  - Phase D (broken modules):
    12,
    13,
    14,
    15,
    16,
    17,
    18,
    19.
  - Phase E (P1 correctness):
    20,
    21,
    22,
    23, 24,
    25,
    26,
    27,
    28,
    29,
    30,
    31,
    32,
    33,
    34, 35,
    36, 37.
  - Phase F (2.0 breaking cleanup):
    38, 39,
    40,
    41,
    42, 43,
    44,
    45,
    46, 47,
    48,
    49,
    50, 51,
    52, 53,
    54.
  - Phase G (documentation):
    55, 56,
    57,
    58,
    59,
    60,
    61.
  - Phase H (Guide conformance):
    62, 63,
    64, 65,
    66, 67,
    68,
    69, 70.
  - Close:
    71.

## Acceptance criteria
- [x] Premortem output (frame, imagined failure, scenario table, top 3 risks, required mitigations naming their step files, accepted risks, decision) recorded in this file.
- [x] Missing checklist files (Hangfire, AUID, Api, Authentication, testing kits) added under `.github/skills/premortem/references/` in the same change.
- [x] `summary.md` `premortem:` updated to the verdict in the same change.

## Open questions
- none

## Premortem

Run on 2026-11-02 in a fresh session that did not author the plan, against `master@4097d47f`
plus the step files as reviewed. Source was read for every persisted contract the plan touches.
Stories were written before the checklists. The sweep used `cqrs.md`, `story.md`, `logging.md`,
`http.md`, `messagebus.md`, `sql.md`, `blob.md`, `cache.md`, `di.md`, `build-and-nuget.md`, and
the five new checklists
([`hangfire.md`](../../../../.github/skills/premortem/references/hangfire.md),
[`auid.md`](../../../../.github/skills/premortem/references/auid.md),
[`api.md`](../../../../.github/skills/premortem/references/api.md),
[`authentication.md`](../../../../.github/skills/premortem/references/authentication.md),
[`testing-kits.md`](../../../../.github/skills/premortem/references/testing-kits.md)).
Rows marked "sweep" came from a checklist.

### Premortem — `SolTechnology.Core 2.0 — flagship hardening (steps 01–70)`

#### Frame

- **Modules touched**: `SolTechnology.Core`, `.CQRS`, `.Tale`, `.AUID`, `.Logging`, `.HTTP`,
  `.API`, `.Authentication`, `.Cache`, `.MessageBus`, `.SQL`, `.Blob`, `.Hangfire`; testing kits
  `.Testing`, `.HTTP.Testing`, `.API.Testing`, `.SQL.Testing`, `.Redis.Testing`, `.Blob.Testing`,
  `.ServiceBus.Testing`; `src/Directory.Build.props`; CI (`.github/runTests.ps1`,
  `.github/workflows/publishPackages.yml`); DreamTravel. `.Guards`/`.Scheduler` only get a
  `GenerateDocumentationFile` opt-out.
- **API delta**:
  - Removed, renamed, or narrowed: `C-02`, `C-03`–`C-24` (brief table), for example
    `ILoggerExtensions` deleted and `CoreLoggingActivitySources.Operations` made internal (`C-20`),
    `Auid` moved out of the global namespace (`C-08`), `IJob.Execute` returns `Task<Result>` (`C-19`).
  - Interface members added: `ITaleRepository` (`C-05`), `IUnitOfWork.Begin` (`C-15`).
  - Added: `TaleJsonOptions` (`C-26`), `MessageBusConfiguration.MaxDeliveryCount` (`C-27`),
    `Result<T>.FailAsTask`, `Auid.New(…, TimeProvider)`, `ITaleBuilder.ExposeOverHttp<T>()`,
    `TaleInstance.Version`, `ChapterInfo.StepIndex`.
  - Behaviour and persisted contracts: `P-01`–`P-17`; log contract: `L-01`–`L-05`.
  - Package graph: `SolTechnology.Core` loses four `Microsoft.Extensions.*` dependencies
    (step 54); SQL and SQL.Testing gain `System.Security.Cryptography.Pkcs 10.0.13` (step 01).
- **Semver impact**: **MAJOR**, which matches the diff. Evidence: public deletions and visibility
  cuts (`C-06`, `C-14`, `C-16`, `C-18`, `C-20`–`C-22`), a namespace move (`C-08`), new interface
  members (`C-05`, `C-15`, `C-19`), and persisted-format breaks (`P-03`, `P-06`, `P-07`).
  `src/Directory.Build.props:5` `1.0.0` → `2.0.0`; `Logging.csproj:4` `1.2.0` → `2.0.0`.
- **Consumers in workspace**: DreamTravel (`sample-tale-code-apps/DreamTravel`, project references,
  so always lockstep). Usings: CQRS 44, Tale 39, Core 18, SQL 6, HTTP 6, Logging 5, API 5,
  Hangfire 4, Cache 3 (local cache only, `DreamTravel.Api/Program.cs:115`,
  `DreamTravel.Worker/Program.cs:51`), Testing 1, Authentication 1. Hangfire is split: the API
  enqueues (`AddSolPersistentEvents`, no server), and the Worker hosts the server and the
  recurring `FetchTrafficJob` (`DreamTravel.Worker/Program.cs`; storage configured with
  `UseSolFilters` in `DreamTravel.Sql/ModuleInstaller.cs:19-24`). External: 20 public packages at
  `1.0.0` (Logging `1.2.0`); `nuget-stats.json` counts CQRS 3,988, Logging 3,595, SQL 3,214,
  Api 2,991, MessageBus 2,832, Authentication 2,136, and Cache 1,494 downloads.

#### Imagined Failure

Six weeks after `v2.0.0`, the freshly transferred flagship repository collects incident reports.
Dependabot opened one PR per package. A consumer merged "bump SolTechnology.Core.Logging to
2.0.0", CI was green, and production threw `FieldAccessException` on the first command, because
CQRS 1.0.0 still reads `CoreLoggingActivitySources.Operations`. A second consumer upgraded Cache,
chose `InstanceName = "orders:"` as the new start-up error demanded, and during the rollout a
retried payment request was charged twice, because every 1.x idempotency record had become
invisible. A third deployed a new build of their own assembly while a checkout tale waited for
input, and the resume re-ran the completed "charge card" chapter, because the history key embeds
`Version=` of a generic chapter type. A fourth runs two workers against one Hangfire database, and
each start deletes the other's recurring jobs.

#### Scenarios

| # | Story (what + mechanism) | Trigger (file:line) | Blast radius | Sev | Lik | Existing control | Mitigation → step |
|---|---|---|---|---|---|---|---|
| 1 | **Partial upgrade at the consumer.** NuGet resolves Logging 2.0 next to CQRS/HTTP/Api/Hangfire/MessageBus 1.x, or AUID 2.0 next to Tale 1.0. The 1.x binaries then hit `CoreLoggingActivitySources.Operations`, which is now internal (`FieldAccessException`), the deleted `ILoggerExtensions` (`MissingMethodException`), or the global-namespace `Auid` (`TypeLoadException`). The consumer's build is green because their own code compiles. | `src/SolTechnology.Core.Logging/Operations/CoreLoggingActivitySources.cs:32`; `src/SolTechnology.Core.CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs:28`; `src/SolTechnology.Core.AUID/Auid.cs` (global namespace) referenced by `SolTechnology.Core.Tale.csproj`; 1.0.0 nuspecs declare open `>=` ranges; steps 44, 52 | public NuGet | H | M (per-package Dependabot PRs) | none — gap (published 1.0.0 nuspecs are immutable) | RM-1 → [04](04-bump-version-2-0-0.md) |
| 2 | **Lock and idempotency keyspace moves.** Without the `InstanceName` default, consumers must pick a value. Any value other than `SolTechnology:` also moves the `lock:` and `idempotency:` keys. During a rolling deploy, 1.x and 2.x instances no longer exclude each other. After the deploy, stored idempotency records are invisible, so a retried request runs twice. P-07 mentions only orphaned cache entries. | `src/SolTechnology.Core.Cache/DistributedCacheConfiguration.cs:6`; `RedisDistributedLockService.cs:26`; `RedisIdempotencyStore.cs:39`; step 29 | public NuGet (DreamTravel uses only the local cache) | H | M | none — gap | RM-2 → 29, 60 |
| 3 | **Identities that carry assembly versions.** `Type.FullName` of a closed generic embeds assembly-qualified arguments with `Version=`/`PublicKeyToken=`. A paused tale with a generic chapter gets a different history key after the consumer redeploys a new assembly version, so the completed chapter re-runs on resume (side effects twice). The same mechanism rotates cache keys for `List<T>` items on every deploy (cold cache, Redis memory growth) and recurring job IDs for generic jobs. | step 42 key `"{stepIndex}:{Type.FullName}"`; step 29 `typeof(TItem).FullName`; step 51 `typeof(TJob).FullName`; today `src/SolTechnology.Core.Tale/Chapter.cs:28` uses `GetType().Name` | public NuGet | H | M | none — gap | RM-3 → 29, 42, 51 |
| 4 | **1.x history cannot be detected.** With a non-nullable `int StepIndex`, every 1.x history entry deserializes as `0`, so the "created by 1.x" guard never fires. A `WaitingForInput` tale then resumes against wrong keys (mis-skip or replay). A test that builds its fixture from 2.x objects passes anyway. | step 42; `src/SolTechnology.Core.Tale/Models/ChapterInfo.cs:16`; `Orchestration/TaleEngine.cs:219-226` | public NuGet; DreamTravel SQLite | H | M | step 42 `Resume_1xHistory_FailsWithDescriptiveError` (effective only on real 1.x JSON) | RM-4 → 42 |
| 5 | **Mixed-version or rolled-back Tale hosts.** 2.x writes `talePaused`/`taleCancelled` discriminators, `StepIndex`, and `Version`. 1.x's closed `[JsonDerivedType]` list throws on an unknown `$type`, so a 1.x host (rolling deploy or rollback) cannot load those tales. A 1.x save also drops `StepIndex`, and 2.x then rejects the tale as "created by 1.x". | `src/SolTechnology.Core/Error.cs:13-21`; steps 17, 41, 42; `DreamTravel.SQLite/SQLiteTaleRepository.cs:271-274` | public NuGet; DreamTravel | M | L | none — gap | RM-5 → 42, 60 |
| 6 | **Hangfire clients upgraded before servers.** In a DreamTravel-style split, a 2.x API enqueues `DispatchInScope(IEvent, CancellationToken)`, and a 1.x Worker cannot resolve that overload. The job fails and is retried. If the rollout outlasts the retry budget, the event ends `Failed`. RQ10 covers only the opposite direction. | `src/SolTechnology.Core.Hangfire/HangfireEventPublisher.cs:42-44`; step 32; DreamTravel API (`AddSolPersistentEvents`) vs Worker (`AddHangfireServer`) | public NuGet; DreamTravel | M | M | Hangfire automatic retry | RM-6 → 32, 61 |
| 7 | **Persisted job identity drifts in a later step.** Steps 51, 57, and 69 edit the Hangfire files. Guide §10 makes a rename of `RunAsync` → `Run` look harmless (the type is internal, so no `C-nn` gate), and so does a move of `HangfireEventPublisher` into a sub-namespace or a rename of the correlation job parameter. Every enqueued, scheduled, or retrying job then fails with "type/method not found". (sweep: hangfire.md) | `src/SolTechnology.Core.Hangfire/RecurringJobRunner.cs:12-13`; `RecurringJobRegistrar.cs:20` (`nameof(...RunAsync)`); `HangfireEventPublisher.cs:33`; `Filters/CorrelationIdJobFilter.cs` (`CorrelationId.ScopeKey` parameter) | public NuGet; DreamTravel Worker | H | M | none — gap (no test pins job identity) | RM-7 → 32, 51, 69 |
| 8 | **Tracked-ID cleanup across apps.** Step 51 removes "registered IDs no longer present" using a storage-wide `sol:recurring-ids` set. Two services that share one Hangfire storage with different job sets delete each other's recurring jobs at every start, and the jobs silently stop. After a rollback to 1.x, the 2.x `FullName` entries stay and fire next to the re-added `Type.Name` entries. | step 51; `src/SolTechnology.Core.Hangfire/RecurringJobRegistrar.cs:21`; `ModuleInstaller.cs` (registrar per process) | public NuGet | H | L | none — gap | RM-8 → 51, 61 |
| 9 | **Recurring-job retry semantics drift.** Smart Retry forces `FailedState`, but `[AutomaticRetry(Attempts = 0)]` stops every retry. If step 51 removes the attribute, thrown exceptions inherit Hangfire's global 10-attempt default, and non-idempotent jobs run up to 11 times. | `src/SolTechnology.Core.Hangfire/Filters/SmartRetryJobFilter.cs:23,39`; `RecurringJobRunner.cs:12`; step 51 | public NuGet; DreamTravel `FetchTrafficJob` | M | M | none — gap | RM-9 → 51, 61 |
| 10 | **`preventOverlap` becomes real and expensive.** The filter loads `ScheduledJobs(0, int.MaxValue)` and `ProcessingJobs(0, int.MaxValue)` on every perform, so each guarded run deserializes the whole scheduled set, including event-job retries. A consumer who does not call `UseSolFilters` gets no protection and no signal. | `src/SolTechnology.Core.Hangfire/Filters/PreventOverlapJobFilter.cs:35,38`; `ModuleInstaller.cs` `UseSolFilters` opt-in; step 13 | public NuGet | M | M | none — gap | RM-10 → 13 |
| 11 | **AUIDs written under a non-Gregorian calendar.** 1.x on th-TH/ar-SA hosts wrote year-shifted strings and stored them as identifiers (columns, URLs, Tale JSON). Step 19's range check rejects them, and step 35 turns that into a `JsonException`, so stored records that hold them fail to load. The release-notes input says such IDs "never round-tripped", which is true for the numeric value but not for the stored string. (sweep: auid.md) | `src/SolTechnology.Core.AUID/Auid.cs:286,291`; `TryParse` range at `:345`; `AuidJsonConverter.cs:20`; steps 19, 35 | public NuGet (non-Gregorian hosts) | H | L | step 19 release-notes input (understated) | RM-11 → 19 |
| 12 | **Blob metadata loss.** 1.x merges existing metadata before re-setting it. A single upload with `BlobUploadOptions.Metadata = { SerializationFormat }` replaces all metadata on overwrite, so consumer keys (owner, retention tags) disappear. There is no test (AR-1). (sweep: blob.md) | `src/SolTechnology.Core.Blob/BlobContainerClientExtensions/BlobContainerClientExtensions.cs:62-74`; step 50 | public NuGet | M | M | none — gap (AR-1) | RM-12 → 50 |
| 13 | **New start-time validators are not in the release notes.** Only `InstanceName` is listed (P-07). Steps 21 (`ValidateDataAnnotations().ValidateOnStart()`), 29 (`ExpirationSeconds > 0`), 33, and 34 make configs that 1.x tolerated fail host start. `CacheConfiguration.ExpirationSeconds` defaults to `0`, and `AddSolLocalCache(config)` copies a partial object as is. (sweep: di.md) | `src/SolTechnology.Core.Cache/CacheConfiguration.cs`; `Cache/ModuleInstaller.cs:20-26`; steps 21, 29, 33, 34 | public NuGet; DreamTravel Worker (`Program.cs:50-51`; no `Cache` section today, so null → defaults) | M | M | `OptionsValidationException` at start (early warning) | RM-13 → 21, 29, 33, 34 |
| 14 | **Silent Tale HTTP opt-in.** After the upgrade, every non-exposed handler returns 404 (P-12), including paused tales the UI must resume, and the server logs nothing. | step 43; `src/SolTechnology.Core.Tale/ModuleInstaller.cs:21,103`; `Api/TaleController.cs:237` | public NuGet; DreamTravel UI | M | H | P-12 release note; DreamTravel UI tests (step 43) | RM-14 → 43 |
| 15 | **Failed `Result`s are now logged.** Step 24 logs every `Result.Fail` at Warning with `[{ErrorMessage}]`. Consumer messages built from input (validation, not-found with an e-mail) reach log sinks that 1.x never fed (PII), and hot validation paths add volume. (sweep: logging.md) | `src/SolTechnology.Core.CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs:55-57`; step 24 | public NuGet; DreamTravel | M | M | L-01 release note | RM-15 → 24, 60 |
| 16 | **Refactors undo pinned contracts.** Steps 62 (`TaleStatePersistence` extraction), 63 (`extension` blocks), 67 (`Auid` partials, Redis service constructors), 68 (MessageBus installer split), and 69 (Hangfire) are "behaviour-preserving", but they touch code that steps 17, 26, 29, 32, 38, 42, and 44 pinned. An "update the test to match" edit erases the guard. | steps 62, 63, 67, 68, 69; `src/SolTechnology.Core.Tale/ModuleInstaller.cs:118-126`; `src/SolTechnology.Core.MessageBus/ModuleInstaller.cs:106` | all | H | L | the earlier steps' tests (only while unmodified) | RM-16 → 62, 63, 67, 68, 69 |
| 17 | **Direct-run pause cannot be resumed.** After step 18, a direct `Handle` of a tale with an interactive chapter still returns a paused result with a `TaleId`, but nothing was persisted, so a later `ResumeStory` returns not-found. 1.x persisted the run in `InMemoryTaleRepository`. (sweep: story.md) | step 18; `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs:91-104,288,442`; `ModuleInstaller.cs:111` | public NuGet | M | L | `docs/Tale.md` "direct handler usage (no persistence)" | RM-17 → 18, 60 |
| 18 | **Symbols lost in the new publish job.** Step 03 moves the push into a job that downloads artifacts. If only `*.nupkg` travels, the 2.0.0 `.snupkg` never reaches nuget.org. (sweep: build-and-nuget.md) | `.github/workflows/publishPackages.yml:73-74`; `src/Directory.Build.props` `IncludeSymbols`/`SymbolPackageFormat`; step 03 | public NuGet | L | M | none — gap | RM-18 → [03](03-publish-only-from-tags.md) |
| 19 | **An empty API key authenticates.** With `ApiKeyAuthenticationSchemeOptions.ApiKey` left at `""`, a request with an empty `X-API-KEY` header passes. Step 07 hashes both values, so the defect ships in 2.0, while step 52 makes `ApiKey` a bindable property that is easy to leave unset. (sweep: authentication.md) | `src/SolTechnology.Core.Authentication/APIKeyAuthenticationHandler.cs:30-36`; `APIKeyAuthenticationSchemeOptions.cs:9`; steps 07, 52 | public NuGet | H | L | none — gap | RM-19 → 07 |
| 20 | **`Result` inside Newtonsoft payloads.** `private init` (step 38) is invisible to Newtonsoft without attributes. A `Result` inside a MessageBus message or a Hangfire job argument therefore deserializes silently as a default failure. (sweep: messagebus.md) | step 38; `src/SolTechnology.Core.MessageBus/Publish/MessagePublisher.cs:5`; `Receive/MessageBusReceiver.cs:7` | public NuGet | M | L | step 38 System.Text.Json round-trip only | RM-20 → 60; accepted |
| 21 | **Confirmed defended — A2 crux.** No workspace consumer reads direct-handler runs from the repository. DreamTravel reaches tales only through `TaleController`/`TaleManager`, and only `SQLiteTaleRepository` calls its own read methods. | `DreamTravel.Api/Controllers/DreamTravelTaleController.cs:9-18`; `SQLiteTaleRepository.cs:135-178` | DreamTravel | M | L | verified here; step 18 tests | none |
| 22 | **Confirmed defended — AUID namespace move and stored JSON.** `AuidJsonConverter.Write` emits the string form, and Tale persistence stores only the short `HandlerTypeName`, never an assembly-qualified name. The binary side of the move is scenario 1. | `src/SolTechnology.Core.AUID/AuidJsonConverter.cs:26-29`; `Tale/Orchestration/TaleManager.cs:169`; `Models/TaleInstance.cs:19` | all | M | L | step 44 test with a `4097d47f` Tale JSON | none |
| 23 | **Confirmed defended — `AddSolTale()` scan regression.** Step 63's `extension` block could drop step 26's `[MethodImpl(NoInlining)]`, so `AddSolTale()` scans the Tale assembly again. | `src/SolTechnology.Core.Tale/ModuleInstaller.cs:118-126`; steps 26, 63 | public NuGet | M | L | `AddSolTale_NoArgs_ScansCallerAssembly` (step 26) | kept unmodified by RM-16 |
| 24 | **Dismissed — `Filters.AddService<T>()` without the installer** (step 22) fails at the first request. `ExceptionFilter` already needs `IExceptionStatusCodeMapper`, which only `AddSolApiExceptionHandling` registers, so no consumer can depend on the old path. (sweep: di.md, api.md) | `src/SolTechnology.Core.Api/ApiCoreMvcOptionsExtensions.cs:41-42`; `Api/ModuleInstaller.cs:146-156` | public NuGet | M | L | DI resolution tests (step 10) | none |
| 25 | **Confirmed defended — `ServiceBusClient` lifetime** (step 12). The keyed singleton never reuses a consumer's client, and the broker no longer disposes the shared client. (sweep: di.md) | step 12; `src/SolTechnology.Core.MessageBus/Broker/MessageBusBroker.cs:42-50` | public NuGet | M | L | `AddSolMessageBus_ConsumerRegisteredServiceBusClient_IsNotReused`, `Broker_DisposeAsync_DoesNotDisposeSharedClient` | none |
| 26 | **Accepted — correlation allow-list** (P-14). Inbound IDs that contain `+`, `/`, `=`, or the legacy hierarchical `Request-Id` form are replaced, which breaks cross-service correlation for those callers. | `src/SolTechnology.Core.Logging/Correlations/CorrelationId.cs:86`; step 31 | public NuGet | L | M | P-14 release note | accepted |
| 27 | **Accepted — L-03 property renames.** `Key` → `IdempotencyKey` and `HealthPath` → `UpstreamURL` leave dashboards on those properties empty. EventIds 2137–2140 and their templates stay untouched (already bracketed). | `src/SolTechnology.Core.Logging/OperationLogMessages.cs:17-53`; step 70 | public NuGet | L | M | L-03 release note | accepted |

#### Top 3 Risks

1. #1 — a per-package upgrade of Logging or AUID to 2.0 next to any sibling package still on 1.x
   crashes at runtime with a green build, and 1.0.0 nuspecs cannot be fixed.
2. #2 — `InstanceName` moves the lock and idempotency keyspace, not just cache keys, so the upgrade
   window executes requests twice.
3. #3 — version-bearing `Type.FullName` keys make a resumed tale re-run completed chapters after
   an ordinary consumer redeploy, and they rotate cache keys and job IDs on every build.

#### Required Mitigations Before Merge

- **RM-1** → [04](04-bump-version-2-0-0.md): pack a `buildTransitive` lockstep check into every
  packable package through `src/Directory.Build.props`. It warns `SOLCORE001` when the resolved
  graph contains a `SolTechnology.Core.*` package (Guards and Scheduler excluded) whose major
  version differs from the package's own. Verify it with a throwaway local-feed consumer. Add the
  release-notes input "upgrade every `SolTechnology.Core.*` package together; group Dependabot or
  Renovate updates for `SolTechnology.Core*`" to the brief.
- **RM-2** → 29,
  60:
  - The P-07 row states that lock and idempotency keys derive from `InstanceName`, and that
    `InstanceName = "SolTechnology:"` keeps the 1.x lock and idempotency keyspace.
  - Test `RedisKeys_LockAndIdempotency_KeepInstanceNamePrefixFormat` pins
    `{InstanceName}lock:{name}` and `{InstanceName}idempotency:{key}`.
  - `docs/Cache.md` says the same.
- **RM-3** → 29,
  42, 51: type-derived cache
  keys, history keys, and recurring job IDs use a version-free type name, never
  `Type.FullName`/`AssemblyQualifiedName` of a generic. Each step adds a generic-type test that
  asserts no `Version=`, `Culture=`, or `PublicKeyToken=` in the key.
- **RM-4** → 42:
  - `ChapterInfo.StepIndex` is `int?`.
  - `Resume_1xHistory_FailsWithDescriptiveError` deserializes a `TaleInstance` JSON literal
    captured at `4097d47f` (status `WaitingForInput`, non-empty `History`) through
    `TaleJsonOptions.Default`.
  - The same fixture asserts that the numeric `TaleStatus` values are unchanged.
- **RM-5** → 42, 60:
  release-notes input and a `docs/Tale.md` migration note: cut Tale hosts over in one step, and do
  not roll back to 1.x once a 2.x host has written tale state.
- **RM-6** → 32,
  61: release-notes input and a `docs/Hangfire.md`
  note: deploy Hangfire servers before processes that only enqueue.
- **RM-7** → 32, 51,
  69: new
  `tests/SolTechnology.Core.Hangfire.Tests/HangfirePersistedIdentityTests.cs` pins:
  - the full names and assembly name of `HangfireEventPublisher` and `RecurringJobRunner<TJob>`;
  - `DispatchInScope` with both parameter lists, and `RunAsync(CancellationToken)`;
  - the correlation job-parameter key.

  Steps 51 and 69 keep it green; 69 leaves it unmodified.
- **RM-8** → 51, 61:
  the tracked-ID set is scoped per application (`IHostEnvironment.ApplicationName`), and removal
  touches only that set. Add test `Registrar_TwoApplicationsSharedStorage_KeepEachOthersJobs`.
  Rollback note: delete the `FullName` IDs after returning to 1.x.
- **RM-9** → 51, 61:
  thrown exceptions in recurring jobs keep 1.x semantics (no automatic retry). Only a recoverable
  `Result` is retried, with an explicit, bounded attempt count on the runner. Tests pin both. If
  that is not achievable, record a deviation, add a `P-nn` row, and state the retry count in
  `docs/Hangfire.md`.
- **RM-10** → 13: the overlap check no longer
  scans `ScheduledJobs`/`ProcessingJobs` with `int.MaxValue`; it uses a check whose cost does not
  grow with storage size, for example a non-blocking storage distributed lock per recurring job
  ID. The registrar logs a Warning at start when a descriptor sets `PreventOverlap` but
  `UseSolFilters` did not install the provider.
- **RM-11** → 19: the release-notes input
  names the affected population (hosts with a non-Gregorian current culture, such as th-TH or
  ar-SA) and the consequence (stored IDs become unreadable). Test
  `TryParse_1xThaiCalendarString_ReturnsFalse` documents it.
- **RM-12** → 50: the single upload still merges existing blob metadata
  (1.x behaviour at `BlobContainerClientExtensions.cs:62-66`). A source check and the manual
  Azurite check cover a custom metadata key that survives an overwrite.
- **RM-13** → 21,
  29,
  33, 34:
  each step appends a `P-nn` row to the brief that names its new validator and the configuration
  that now fails at start. Step 29 also pins with a test what
  `AddSolLocalCache(new CacheConfiguration { ExpirationMode = … })` without seconds does.
- **RM-14** → 43: when the controller rejects a handler that
  was scanned but not exposed, it logs a Warning that names the handler and `ExposeOverHttp<T>()`.
  The 404 body carries no hint. Test `Start_ScannedButNotExposed_Returns404AndLogsWarning`.
- **RM-15** → 24, 60:
  the failure entry carries only the `Error` runtime type name and `Error.Message`, with no
  `ValidationError` detail collection and no `Data`. Test it. `docs/Log.md` warns not to put PII in
  `Error.Message`.
- **RM-16** → 62, 63,
  67, 68,
  69: the pinned-contract tests pass with no diff to
  their test files. Each step lists its guards.
- **RM-17** → 18,
  60: a run that pauses outside `TaleManager` logs a
  Warning that it is not persisted and cannot be resumed. Test it, and document it in
  `docs/Tale.md`.
- **RM-18** → [03](03-publish-only-from-tags.md): the uploaded artifact and the push include
  `*.snupkg`.
- **RM-19** → 07: a null, empty, or whitespace
  configured `ApiKey` never authenticates. Add the test case to the matrix.
- **RM-20** → 60: `docs/Core.md` states that the
  `Result`/`Error` JSON contract is System.Text.Json only, and that Newtonsoft payloads
  (MessageBus messages, Hangfire job arguments) must not carry `Result`.

#### Accepted Risks

- #5 — the single-version cutover and no-rollback rule is documented (RM-5), not enforced; a
  library cannot see the consumer's deployment topology.
- #11 — after RM-11, hosts that ran 1.x under a non-Gregorian culture still have to migrate their
  stored IDs; likelihood is low, and accepting the corrupt form would keep `B11` open.
- #20 — `Result` in Newtonsoft payloads is documented as unsupported (RM-20). Adding Newtonsoft
  attributes would put a dependency into the zero-dependency base package (step 54).
- #26 — the P-14 allow-list is a deliberate security trade-off; the release note covers it.
- #27 — the L-03 renames are accepted under D2; the release note covers them, and the EventIds stay.
- RM-1 residual — consumers who suppress `SOLCORE001`, or who restore from a lock file without
  the 2.0 assets, rely on the release note alone.
- The brief's AR-1 to AR-5 stand unchanged. AR-1 now carries RM-12's manual check.

#### Decision

**Go with mitigations** — every `H` scenario has a concrete mitigation in a named step, the
MAJOR classification matches the diff, and no open item needs a user-only decision.

## Deviations

### Premortem skill table lists the new checklists

**Original plan:** add the five missing checklist files under
`.github/skills/premortem/references/`.

**Actual implementation:** the files were added, and five rows were added to the module table in
`.github/skills/premortem/SKILL.md`, so the next premortem loads them; an unlisted reference file
is never read.

### Step numbers in this record are pre-regroup numbers (2026-11-02)

**Original plan:** this premortem names steps `01`–`71` of this feature and links to their files.

**Actual implementation:** on 2026-11-02 the user split the implementation into one feature per
library ("podziel implementację feature 1 na każdą bibliotekę osobno"). Steps 05–70 moved to the
library features or were renumbered here, and their former files were deleted. The step numbers in
this record stay as written (this output is historical); links to moved files became plain numbers.
The "Per-library regroup" mapping table in the feature brief translates each number, and each
mitigation `RM-1`…`RM-20` was carried verbatim into the step that now holds it. The verdict covers
the regrouped steps unchanged: as decided by the user, the library features record
`premortem: skipped (covered by core premortem go-with-mitigations 2026-11-02 …)`, or
`waived (…)` where no `CLAUDE.md §4` trigger applies.

