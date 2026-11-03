---
status: completed
created: 2026-11-02
completed: 2026-11-03
---

# SolTechnology.Core 2.0 — Logging

> Historical delivery record. It may not describe the current system.

## Goal

Deliver the 2.0 hardening of `SolTechnology.Core.Logging` end-to-end, as one library of the SolTechnology.Core 2.0
flagship release. Success means:

- `AddSolLogging(IConfiguration)` registers everything its middleware needs (`B4`), so the module resolves from DI under `ValidateOnBuild`.
- The middleware matches excluded paths per segment and writes one Error entry per failed request (P-15); inbound correlation IDs are sanitized (P-14); timings and masking accept unsafe input.
- The public surface follows Guide §10 and §11: `LoggerExtensions` with an `extension(ILogger)` block, internal `CoreLoggingActivitySources.Operations`, and CQRS on its own `ActivitySource` (`C-20`, RQ9).
- `docs/Log.md` compiles against 2.0, and Logging conforms to the Coding Guide.

## Context

**Depends on:**

- Core 02, 03, and 04 of [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) are `done` (CI fails on any test failure, tag-only publishing, version 2.0.0 on `master`).
- No other library feature: this library references no package that changes in another library feature.

Delivery order position 2 of 13 in the umbrella brief's "Per-library regroup" section. The
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md) feature
depends on this feature being `completed`.

### Origin

Regrouped on 2026-11-02 from [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) (user: "podziel implementację feature 1 na
każdą bibliotekę osobno"). Each step names its former core step in a "Former core step" line; the
umbrella brief's "Per-library regroup" table maps every former number. The evidence is the
full-repository code review of 2026-11-02 against `master@4097d47f`; finding IDs follow the
umbrella brief's "Evidence" table. Findings acted on here: `B4`, `P1-2`, `cqrs-tale#18`, `logging-cache#14`, `logging-cache#18`, `logging-cache#19`, `logging-cache#21`, `logging-cache#23`, `logging-cache#25`, `logging-cache#26`.

### Decisions owned by this feature

The umbrella brief keeps the shared decision tables and the user's confirmations (`C-01`…`C-24`,
`C-26`, `C-27` confirmed, `C-25` rejected, 2026-11-02). The rows below are copied from it. Step
numbers in copied `A`, `AR`, `Q`, and `RQ` rows are former core numbers.

Public API changes (`CLAUDE.md §2`, confirmed by the user on 2026-11-02):

| ID | Package | Exact change | Step |
|---|---|---|---|
| C-20 | Logging | `ILoggerExtensions` (`ILoggerExtensions.cs:12`) → `LoggerExtensions` with an `extension(ILogger logger)` block; `CoreLoggingActivitySources.Operations` (`CoreLoggingActivitySources.cs:32`) → `internal`; `OperationsName` stays public and CQRS creates its own `ActivitySource` with that name (RQ9) | logging 05 |

Behavioural and persisted-contract breaks:

| ID | Change | Consumer impact | Step |
|---|---|---|---|
| P-14 | Inbound `X-Correlation-Id` outside `[A-Za-z0-9._:-]` is replaced | Clients sending such IDs see a new ID | logging 04 |
| P-15 | A failed request logs one Error entry, not two | Error-rate alerts change | logging 03 |

User decisions (2026-11-02):

| ID | Question | Decision (user, 2026-11-02) | Steps |
|---|---|---|---|
| RQ9 | `CoreLoggingActivitySources.Operations` read by CQRS | **(a)** CQRS creates its own `ActivitySource` named `OperationsName`; the field becomes internal. | 52 |

Premortem mitigations carried verbatim from core 00: none carried; `docs/Log.md` keeps the RM-15 note written by cqrs 05.

Logging already ships `GenerateDocumentationFile` (`SolTechnology.Core.Logging.csproj:15`), so this feature has no XML-docs step; core 08 moves that setting into `src/Directory.Build.props`. Logging has no L-03 template row; step 07 runs the library-scoped `[{Placeholder}]` check.

## Scope

### In scope

- Source: `src/SolTechnology.Core.Logging`, and the matching test projects; plus the CQRS call site
  `src/SolTechnology.Core.CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs` and `tests/SolTechnology.Core.CQRS.Tests`
  (step 05, RQ9: making `CoreLoggingActivitySources.Operations` internal breaks the CQRS build otherwise).
- Module docs: `docs/Log.md`.
- Phases, in order: correctness and broken-module fixes → 2.0 breaking API cleanup → XML docs and
  module docs → Coding Guide conformance and `[{Placeholder}]` log templates → retrospective.

### Out of scope

- Chronicle step reports (`2026-11-02-logging-chronicle`), a separate feature that builds on this one.
- Splitting the CQRS → Logging → ASP.NET Core shared-framework dependency (`cqrs-tale#18`, second half).
- Work owned by the foundation feature (CI, publishing, version, DreamTravel exception page, secret
  artifacts, zero-dependency base package, central `GenerateDocumentationFile`), the other library
  features, the showcase feature, and the release feature.

## Implementation plan

Steps lived in the temporary working folder `2026-11-02-core-2-0-logging`, which the retrospective
deleted on 2026-11-03. Gates: `review` was `done (2026-11-02)`; `premortem` was `skipped` — the core
premortem verdict `go-with-mitigations (2026-11-02)` covered these steps.

Plan review (2026-11-02): the regrouping was checked against the pre-regroup plan (`dddac21c`) and
found complete. User answers recorded, not re-asked: the delivery order with AUID before Tale is
confirmed (Tale references AUID); the per-library split, the premortem reuse, and the `C-01`…`C-24`,
`C-26`, `C-27` confirmations stand (`C-25` rejected). Review fixes applied here: `In scope` names the CQRS call-site edit of step 05 (RQ9); the retrospective's DreamTravel test command is runnable.

Notation: an unqualified step number is a step of this feature; `core NN` names step `NN` of
[`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md); other short names (`result`, `logging`, `cqrs`, `auid`, `tale`, `http`, `api-auth`,
`cache`, `sql`, `blob`, `messagebus`, `hangfire`, `testing-kits`) name the library features listed
in the umbrella brief; `release NN` names a step of
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md).

| Step | Title | Former core step(s) |
|---|---|---|
| 01 | DI resolution test — Logging | 10 |
| 02 | Logging — `AddSolLogging(IConfiguration)` registers its middleware dependencies | 14 |
| 03 | Logging — segment-aware path matching; one Error entry per failure | 30 |
| 04 | Logging — sanitize inbound correlation IDs; safe timings and masking | 31 |
| 05 | Logging — public surface cleanup (`LoggerExtensions`, internal `Operations`) | 52 |
| 06 | Module docs — `docs/Log.md` | 60 |
| 07 | Guide conformance — Logging | 66, 67, 70 (library-scoped check) |
| 08 | Retrospective | 71 |

Steps run in number order; each is one PR.

## Acceptance criteria

- [x] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations`.
- [x] The `B4` regression test (resolution through `AddSolLogging(IConfiguration)`) fails on `4097d47f` and passes; the Logging resolution test is not `[Ignore]`d.
- [x] `C-20` shipped: `LoggerExtensions` replaces `ILoggerExtensions`; `CoreLoggingActivitySources.Operations` is internal; `OperationsName` stays public; CQRS creates its own `ActivitySource` named `OperationsName`.
- [x] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.Logging.Tests` and `dotnet test tests/SolTechnology.Core.CQRS.Tests` are green.
- [x] The module resolution test uses `ValidateOnBuild = true` and `ValidateScopes = true`, and no resolution test is `[Ignore]`d.
- [x] `find src/SolTechnology.Core.Logging -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] Every packed `.nupkg` of this feature contains `lib/net10.0/*.xml`.
- [x] Every C# snippet in `docs/Log.md` compiles in a throwaway console project against the 2.0 API, as recorded in the module-docs step.
- [x] The library-scoped `[{Placeholder}]` template check of the conformance or log-template step prints nothing.

## Completion summary

Delivered on `master` between `dab07639` and `ae7b0ab2` (2026-11-03); every step's acceptance
criteria were verified with the commands they name. Current behaviour and rationale live in
[`docs/architecture/logging.md`](../architecture/logging.md); consumer usage in [`docs/Log.md`](../Log.md).

- **01 / 02 — B4 fixed.** `ModuleResolutionTests` builds a provider with `ValidateOnBuild` and
  `ValidateScopes` for both `AddSolLogging` overloads and sends a request through `UseSolLogging()`.
  With the step-01 `[Ignore]` removed it failed with "Unable to resolve service for type
  'ITimingService' while attempting to Invoke middleware 'LoggingMiddleware'" (the Logging sources
  were unchanged since `4097d47f`). Both overloads now share one registration path; a `TestServer`
  request after `AddSolLogging(configuration)` returns 200. No resolution test is ignored.
- **03 — P-15 and segment matching.** `SkipPaths` and `LogDetail` endpoints match per path segment
  through one `LoggingPathMatcher`. A failed request writes one Error entry and no `Finished` entry.
- **04 — P-14, timings, masking.** Inbound `X-Correlation-Id` values outside `[A-Za-z0-9._:-]` are
  replaced; `GetTimings()` returns a read-only snapshot; `PiiMask.Partial` throws on a negative
  `keepChars` and reveals nothing unless the value is longer than `4 * keepChars`. Return types are
  unchanged.
- **05 — C-20.** `ILoggerExtensions` became `LoggerExtensions` with an `extension(ILogger)` block;
  `CoreLoggingActivitySources.Operations` is internal and `OperationsName` stays public; CQRS emits on
  its own `ActivitySource` named `OperationsName` (RQ9). No `InternalsVisibleTo` entry was added.
- **06 — `docs/Log.md`.** Describes the 2.0 behaviour; every C# snippet compiled against the 2.0
  API in a throwaway project, and three that did not compile were fixed.
- **07 — Guide conformance.** No Logging file exceeds 150 lines (largest: `LoggingMiddleware.cs`
  150, `CorrelationId.cs` 146); primary constructors, `extension` blocks, braces, AAA test names; no
  unbracketed placeholder, so the core brief's L-03 entry needs no Logging property. ApiCompat found
  no breaking change for the step.
- **08 — Retrospective.** `dotnet build SolTechnology.Core.slnx -c Release`: 0 errors (101
  pre-existing warnings, none in Logging or CQRS); the NU1901–NU1904 / NU1605 grep prints nothing;
  `dotnet test SolTechnology.Core.slnx`: 18 projects, 555 passed, 0 failed, 11 skipped (pre-existing
  skips in `DreamTravel.GeolocationDataClients.IntegrationTests`); Logging.Tests 78, CQRS.Tests 39 and
  the DreamTravel component and unit projects green. The packed `SolTechnology.Core.Logging.2.0.0.nupkg`
  contains `lib/net10.0/SolTechnology.Core.Logging.xml` and the updated readme.

Public API changes made: `C-20` only (confirmed by the user on 2026-11-02). Changes to the core
brief's decision rows and new release-notes inputs for release 05:

- **P-15 (amended):** a client abort also writes only its Warning entry, with no `Finished` entry.
- **P-14 (amended):** multi-valued `X-Correlation-Id` headers (joined with `,`) are replaced too; the
  128-character limit is unchanged.
- **New behaviour notes:** `SkipPaths` and `LogDetail` endpoints match whole path segments
  (`/api/v1/Trip` no longer covers `/api/v1/Trips`); `AddSolLogging(IConfiguration)` now works
  (before 2.0 it threw on every request); `ITimingService.GetTimings()` returns a read-only snapshot
  (mutating it throws `NotSupportedException`); `PiiMask.Partial` and `[Masked(MaskMode.Partial)]`
  mask values of `4 * keepChars` characters or fewer completely and throw for a negative `keepChars`;
  `LoggingMiddleware` entries carry event ids 2141–2148 instead of 0 (templates unchanged).
- **C-20:** as planned; static calls `ILoggerExtensions.X(logger, …)` must become
  `LoggerExtensions.X(logger, …)` or `logger.X(…)`, and code that read
  `CoreLoggingActivitySources.Operations` subscribes with `AddSource(OperationsName)` instead.
- **L-03:** unchanged for Logging (no unbracketed placeholder found).

## Deviations

- **Step 02 — shared path owns option validation.** `RegisterCore()` also creates the validated
  `LoggingOptions` builder that both overloads duplicated; binding and rules are unchanged. Step 07
  moved it into the internal `LoggingServiceRegistrations`.
- **Step 03 — matcher extracted early; aborts skip Finished.** `LoggingPathMatcher` (planned for
  step 07) was introduced in step 03 so the middleware and `LogDetailEnricher` share one segment rule;
  it ignores a trailing `/` and prefixes without a leading `/`. The "no Finished after a logged
  exception" rule also covers client aborts. A new `LogDetailEnricherTests` pins the endpoint rule.
- **Step 04 — tests.** The correlation test is named after `CorrelationId.FromRequest` and also
  covers spaces and `,`; two `PiiMaskTests` that pinned the old 2 × `keepChars` rule now use longer
  inputs.
- **Step 05 — CQRS source holder.** The CQRS `ActivitySource` lives in a new internal, non-generic
  `OperationsActivitySource` instead of a static field on the generic `LoggingPipelineBehavior<,>`
  (which would create one source per closed type). The listener test created
  `tests/SolTechnology.Core.CQRS.Tests/PipelineBehaviors/LoggingPipelineBehaviorTests.cs`.
- **Step 06 — accuracy fixes.** Besides the planned updates, `docs/Log.md` fixed three
  non-compiling snippets (`APIFixture.ServerClient`, two scope variable names) and two inaccurate
  statements (`APIFixture` does not wire Logging; `CoreLoggingActivitySources` is not a DI service).
- **Step 07 — extraction set and pins.** Pin tests landed first in three new files so the step
  01–05 test files have no diff (premortem RM-16). `RequestEnvelopeLogger` also owns the Finished
  level choice and the body-parse Debug entry, with event ids 2141–2148. `CorrelationId.cs` needed
  the traceparent guard simplified and an XML-doc essay cut, besides `CorrelationIdValidator`, to get
  under 150 lines. `LoggingMiddleware` also got a primary constructor and an accurate summary. The
  planned placeholder regex cannot see `[LoggerMessage]` templates, so a tuned regex was run as well.
- **Retrospective — `PushToScope(params)` stays a classic extension method.** Step 07 moved it into
  an `extension` block, which made the SDK 10.0.100 compiler report CS8620 at every tuple call site
  (`logger.PushToScope(("A", "1"))`) — a break for consumers that treat warnings as errors. It is a
  classic `this` method again (`9c67ecc8`); Logging.Tests treats CS8620 as an error to guard it, and
  Coding Guide §9.7 records the exception.
- **Packaging criterion scope.** "Every packed `.nupkg` of this feature contains
  `lib/net10.0/*.xml`" holds for `SolTechnology.Core.Logging`. `SolTechnology.Core.CQRS` (touched only
  at its call site in step 05) still packs no XML documentation; that is owned by core 08 and the
  CQRS feature.

## Follow-ups

- Split the CQRS → Logging → ASP.NET Core shared-framework dependency (`cqrs-tale#18`, second half).
- cqrs step 03: `LoggingPipelineBehaviorTests.cs` already exists (logging 05); extend it instead of
  creating it. cqrs step 06 still owns the hand-written constructor in `LoggingPipelineBehavior.cs`.
- Move `LoggerScopeExtensions.PushToScope(params)` into the `extension` block and drop the Guide §9.7
  exception once the compiler stops reporting CS8620 for tuple arguments there.
- `LoggingMiddlewareTests`, `CorrelationIdTests` and other pre-existing Logging tests still use
  snake_case names; rename them in a test-only change now that RM-16 no longer freezes them.
- Release 05: take the amended P-14 / P-15 rows and the new behaviour notes above into the 2.0
  release notes and migration guide.

## Post-completion review fixes (2026-11-03)

An independent code review of the completed feature found four issues; the fixes are on `master`.

- **L1 — finding:** `CorrelationId.FromRequest` preferred `Activity.Current` unconditionally. ASP.NET
  Core starts a request `Activity` whenever logging is enabled, so in every real host a caller's
  valid `X-Correlation-Id` was replaced by a fresh trace id (the `docs/Log.md` component-test snippet
  could not pass). **Fix (user decision, 2026-11-03):** the `Activity` trace id wins only when the
  request carried trace context (a `traceparent` header or an `Activity` with a remote parent);
  otherwise a valid `X-Correlation-Id` is used and echoed; otherwise the `Activity` trace id;
  otherwise a generated id. The `TryBuildTraceParent` guard is unchanged, so no span id is invented
  for a header id. **Tests:** `CorrelationIdHostTests` (HostBuilder + TestServer with an enabled
  logging provider, so hosting creates the `Activity`):
  `Request_WithOnlyCorrelationHeader_UsesAndEchoesHeader` failed before the fix (the response echoed
  the trace id) and passes; `Request_WithTraceParent_UsesInboundTraceId` passes before and after.
  `CorrelationIdTests.FromRequest_prefers_activity_trace_id_over_header` pinned the old order and is
  replaced by `FromRequest_ValidHeaderWithoutTraceContext_UsesHeaderOverLocalActivity` (failed
  before), `FromRequest_TraceParentHeader_UsesActivityTraceIdOverHeader`,
  `FromRequest_ActivityWithRemoteParent_UsesActivityTraceIdOverHeader`, and
  `FromRequest_NoUsableHeader_FallsBackToActivityTraceId` (the invalid-header case failed before:
  the header was not even validated while an `Activity` existed). `docs/Log.md` (resolution order,
  the `Endpoint_AcceptsCorrelationId_AndEchoesItBack` snippet now sends a plain support id and
  asserts no `traceparent`) and `docs/architecture/logging.md` updated. Release-notes input: amends
  P-14 (umbrella brief).
- **L2 — finding:** `RequestBodyBuffer` caught only `JsonException` and `OperationCanceledException`;
  a client disconnecting mid-upload (`ConnectionResetException`, `BadHttpRequestException`, both
  `IOException`) escaped scope building, so the request failed without any envelope entry. **Fix:**
  `IOException` is treated like cancellation — body enrichment is skipped and the pipeline's own
  body read reports the abort. **Test:**
  `LoggingMiddlewareBodyBufferingTests.Invoke_ClientDisconnectsDuringBodyRead_LogsSingleWarningWithoutFinished`
  (body stream throws `ConnectionResetException`): failed before (no entries at all), passes with one
  Warning (2144) and no Finished entry.
- **L3 — finding:** `TimingService.GetTimings()` allocated two dictionaries per request even when
  empty, enumerated the live `ConcurrentDictionary`, and `RequestFinished` built the snapshot even
  when its level was disabled. **Fix:** a shared `ReadOnlyDictionary<string, long>.Empty` when no
  timing was recorded, the snapshot built from `_timings.ToArray()`, and an early return in
  `RequestFinished` when the level is disabled. Behaviour unchanged. **Tests:**
  `TimingServiceTests.GetTimings_WhenEmpty_ReturnsSharedReadOnlyInstance` and
  `RequestEnvelopeLoggerTests.RequestFinished_LevelDisabled_DoesNotReadTimings` failed before and pass.
- **L4 — finding:** the abort path had no envelope test, and the event ids 2141–2148 were not
  asserted. **Fix (tests only):** `LoggingMiddlewareEnvelopeTests` captures `EventId` and asserts it
  in every test; `Invoke_ClientAbortedRequest_LogsSingleWarningWithoutFinished` (cancelled
  `RequestAborted` → one Warning 2144, no Finished) and
  `Invoke_MalformedJsonBodyForBodyEnricher_LogsDebugSkipTemplate` (2148) are new. Both pass before
  and after (coverage of existing behaviour).
