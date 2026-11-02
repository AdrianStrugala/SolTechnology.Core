---
status: planned
created: 2026-11-02
completed:
---

# SolTechnology.Core 2.0 — Core `Result` and `Error`

> Historical delivery record. It may not describe the current system.

## Goal

Deliver the 2.0 hardening of `SolTechnology.Core` end-to-end, as one library of the SolTechnology.Core 2.0
flagship release. Success means:

- `Error` serializes every subtype: unknown consumer subtypes fall back to their nearest ancestor instead of throwing, and `AggregateError` round-trips (`B7`, A1).
- `Result` and `Result<T>` are immutable after construction, and `Result<T>.FailAsTask` is typed (`C-02`).
- The `SolTechnology.Core` package ships XML docs, a `docs/Core.md` README that compiles against 2.0, and conforms to the Coding Guide.

## Context

**Depends on:**

- Core 02, 03, and 04 of [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) are `done` (CI fails on any test failure, tag-only publishing, version 2.0.0 on `master`).
- No other library feature: this library references no package that changes in another library feature.

Delivery order position 1 of 13 in the umbrella brief's "Per-library regroup" section. The
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md) feature
depends on this feature being `completed`.

### Origin

Regrouped on 2026-11-02 from [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) (user: "podziel implementację feature 1 na
każdą bibliotekę osobno"). Each step names its former core step in a "Former core step" line; the
umbrella brief's "Per-library regroup" table maps every former number. The evidence is the
full-repository code review of 2026-11-02 against `master@4097d47f`; finding IDs follow the
umbrella brief's "Evidence" table. Findings acted on here: `B7`, `cqrs-tale#3`, `cqrs-tale#16`, `cqrs-tale#17`, `cqrs-tale#21`, `cqrs-tale#25`, `repo#A9`, `P1-4`.

### Decisions owned by this feature

The umbrella brief keeps the shared decision tables and the user's confirmations (`C-01`…`C-24`,
`C-26`, `C-27` confirmed, `C-25` rejected, 2026-11-02). The rows below are copied from it. Step
numbers in copied `A`, `AR`, `Q`, and `RQ` rows are former core numbers.

Public API changes (`CLAUDE.md §2`, confirmed by the user on 2026-11-02):

| ID | Package | Exact change | Step |
|---|---|---|---|
| C-02 | Core | `Result.IsSuccess { get; init; }`, `Result.Error { get; init; }`, `Result<T>.Data { get; init; }` (`Result.cs:11,17,52`) → `private init`; NEW `Result<T>.FailAsTask(Error)` returning `Task<Result<T>>` | result 02 |

Behavioural and persisted-contract breaks:

| ID | Change | Consumer impact | Step |
|---|---|---|---|
| P-01 | `Error` subtypes not registered serialize as their nearest ancestor; Tale errors gain discriminators through `TaleJsonOptions.Default`, which `TaleInstance.Clone()` and the DreamTravel SQLite repository now use | JSON shape for unknown subtypes; no more `NotSupportedException`; custom repositories must use `TaleJsonOptions.Default` to keep Tale error types (`C-26`) | result 01, tale 02 |

Design alternatives (blue-red-team, 2026-11-02):

| # | Choice | Alternatives | Recommendation and crux |
|---|---|---|---|
| A1 | `Error` polymorphism (`B7`, `src/SolTechnology.Core/Error.cs:13-22`) | (a) add Tale errors to the closed `[JsonDerivedType]` list (impossible: `SolTechnology.Core` cannot reference Tale); (b) `[JsonPolymorphic(UnknownDerivedTypeHandling = FallBackToNearestAncestor)]` only; (c) (b) plus a Tale-owned `TypeInfoResolver` modifier in `TaleJsonOptions.Default` that registers `TaleCancelledError`/`TalePausedError` discriminators | **(c).** Blue: no throw for consumer subtypes, Tale errors round-trip in persistence. Red: unknown consumer subtypes lose their runtime type on deserialize. Crux: does any consumer rely on round-tripping its own subtype? No evidence in the workspace, so fallback is acceptable and documented. |

Premortem mitigations carried verbatim from core 00: RM-20 (step 04).

## Scope

### In scope

- Source: `src/SolTechnology.Core`, and the matching test projects.
- Module docs: `docs/Core.md`.
- Phases, in order: correctness and broken-module fixes → 2.0 breaking API cleanup → XML docs and
  module docs → Coding Guide conformance and `[{Placeholder}]` log templates → retrospective.

### Out of scope

- The Tale half of `B7`/P-01 (public `TaleJsonOptions.Default` with Tale discriminators): tale 02.
- Removing `Microsoft.Extensions.*` from `SolTechnology.Core` (P-13): core 07.
- Work owned by the foundation feature (CI, publishing, version, DreamTravel exception page, secret
  artifacts, zero-dependency base package, central `GenerateDocumentationFile`), the other library
  features, the showcase feature, and the release feature.

## Implementation plan

Steps live in the temporary working folder `2026-11-02-core-2-0-core-result/` (see its `summary.md`). Gates: `review` is
`pending` (breaking public API or persisted contract, so the planner cannot waive it; the user may
skip it); `premortem` is `skipped` — the core premortem verdict `go-with-mitigations (2026-11-02)`
covers these steps.

Notation: an unqualified step number is a step of this feature; `core NN` names step `NN` of
[`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md); other short names (`result`, `logging`, `cqrs`, `auid`, `tale`, `http`, `api-auth`,
`cache`, `sql`, `blob`, `messagebus`, `hangfire`, `testing-kits`) name the library features listed
in the umbrella brief; `release NN` names a step of
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md).

| Step | Title | Former core step(s) |
|---|---|---|
| 01 | Core — open `Error` polymorphism; `AggregateError` round-trip | 17 |
| 02 | Core — immutable `Result` and a typed `Result<T>.FailAsTask` | 38 |
| 03 | XML docs shipped — Core | 55 |
| 04 | Module docs — `docs/Core.md` as the `SolTechnology.Core` package README | 60 |
| 05 | Guide conformance — Core | 66, 70 (library-scoped check) |
| 06 | Retrospective | 71 |

Steps run in number order; each is one PR.

## Acceptance criteria

- [ ] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations`.
- [ ] Regression tests for `B7` (unknown `Error` subtype, `AggregateError` round-trip) fail on `4097d47f` and pass.
- [ ] `C-02` shipped: `Result.IsSuccess`, `Result.Error`, and `Result<T>.Data` have `private init`; `Result<T>.FailAsTask(Error)` returns `Task<Result<T>>`.
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Tests` is green.
- [ ] `find src/SolTechnology.Core -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] Every packed `.nupkg` of this feature contains `lib/net10.0/*.xml`.
- [ ] Every C# snippet in `docs/Core.md` compiles in a throwaway console project against the 2.0 API, as recorded in the module-docs step.
- [ ] The library-scoped `[{Placeholder}]` template check of the conformance or log-template step prints nothing.

## Completion summary

## Deviations

## Follow-ups
