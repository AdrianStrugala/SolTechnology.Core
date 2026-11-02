---
status: planned
created: 2026-11-02
completed:
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

Steps live in the temporary working folder `2026-11-02-core-2-0-logging/` (see its `summary.md`). Gates: `review` is
`done (2026-11-02)`; `premortem` is `skipped` — the core premortem verdict `go-with-mitigations (2026-11-02)`
covers these steps.

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

- [ ] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations`.
- [ ] The `B4` regression test (resolution through `AddSolLogging(IConfiguration)`) fails on `4097d47f` and passes; the Logging resolution test is not `[Ignore]`d.
- [ ] `C-20` shipped: `LoggerExtensions` replaces `ILoggerExtensions`; `CoreLoggingActivitySources.Operations` is internal; `OperationsName` stays public; CQRS creates its own `ActivitySource` named `OperationsName`.
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` and `dotnet test tests/SolTechnology.Core.CQRS.Tests` are green.
- [ ] The module resolution test uses `ValidateOnBuild = true` and `ValidateScopes = true`, and no resolution test is `[Ignore]`d.
- [ ] `find src/SolTechnology.Core.Logging -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] Every packed `.nupkg` of this feature contains `lib/net10.0/*.xml`.
- [ ] Every C# snippet in `docs/Log.md` compiles in a throwaway console project against the 2.0 API, as recorded in the module-docs step.
- [ ] The library-scoped `[{Placeholder}]` template check of the conformance or log-template step prints nothing.

## Completion summary

## Deviations

## Follow-ups
