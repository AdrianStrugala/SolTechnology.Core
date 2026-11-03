---
status: completed
created: 2026-11-02
completed: 2026-11-03
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

Steps lived in a temporary working folder, deleted at completion (2026-11-03; see Git history). Gates: `review` is
`done (2026-11-02)`; `premortem` is `skipped` — the core premortem verdict `go-with-mitigations (2026-11-02)`
covers these steps.

Plan review (2026-11-02): the regrouping was checked against the pre-regroup plan (`dddac21c`) and
found complete. User answers recorded, not re-asked: the delivery order with AUID before Tale is
confirmed (Tale references AUID); the per-library split, the premortem reuse, and the `C-01`…`C-24`,
`C-26`, `C-27` confirmations stand (`C-25` rejected). Review fixes applied here: step 02 adds the missing `ProjectReference` from `tests/SolTechnology.Core.Tests` to `src/SolTechnology.Core` (the project held only guard tests); the brief's test criterion also names `tests/SolTechnology.Core.CQRS.Tests`, where `ErrorSerializationTests` lives; the retrospective's DreamTravel test command is runnable.

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

- [x] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations`.
- [x] Regression tests for `B7` (unknown `Error` subtype, `AggregateError` round-trip) fail on `4097d47f` and pass.
- [x] `C-02` shipped: `Result.IsSuccess`, `Result.Error`, and `Result<T>.Data` have `private init`; `Result<T>.FailAsTask(Error)` returns `Task<Result<T>>`.
- [x] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.Tests` and `dotnet test tests/SolTechnology.Core.CQRS.Tests` (home of `ErrorSerializationTests`) are green.
- [x] `find src/SolTechnology.Core -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] Every packed `.nupkg` of this feature contains `lib/net10.0/*.xml`.
- [x] Every C# snippet in `docs/Core.md` compiles in a throwaway console project against the 2.0 API, as recorded in the module-docs step.
- [x] The library-scoped `[{Placeholder}]` template check of the conformance or log-template step prints nothing.

## Completion summary

Delivered on `master` between `81288e4f` and the retrospective commit (2026-11-03):

- **01:** `Error` falls back to the nearest registered ancestor for unknown subtypes (no more
  `NotSupportedException`); `AggregateError.InnerErrors` survives a JSON round-trip. Nine snapshot
  tests pin the JSON of every existing discriminator (unchanged).
- **02 (`C-02`):** `Result`/`Result<T>` state is read-only outside the factories and implicit
  conversions; `Result<T>.FailAsTask(Error)` returns `Task<Result<T>>`. JSON written by 1.x
  still deserializes and 2.0 writes the same shape.
- **03:** `SolTechnology.Core` ships its XML documentation; every public member is documented.
- **04:** `docs/Core.md` is the package README (snippets compiled and run against 2.0).
- **05:** Guide conformance; `AggregateError` message building is pinned by tests and braced.
- **Retrospective** (closure confirmed by the user on 2026-11-03 for every library feature): current behaviour and rationale are in
  [`docs/architecture/core-result-and-errors.md`](../architecture/core-result-and-errors.md).
  Final validation: `dotnet build SolTechnology.Core.slnx -c Release` green with no
  `NU190[1-4]`/`NU1605`; `dotnet test SolTechnology.Core.slnx -c Release` 519 passed, 0 failed.

Changes to core-brief rows and release-notes inputs:

- `C-02`: `IsSuccess` and `Error` are `private protected init`, not `private init`, because
  `Result<T>`'s factories set them; the effect for consumers is the same.
- Release notes: OpenAPI schemas of `Result` types now mark `isSuccess` and `data` as `readOnly`.
- Release notes (P-01): custom `Error` subtypes serialize as `$type: "error"` with only the base
  properties.

## Deviations

- **01:** the `AggregateError` JSON constructor is `private`, not public, so it cannot win overload
  resolution over the existing public `(string, IEnumerable<Error>)` constructor.
- **02:** `private protected init` on the base properties (see above); the malformed-result test in
  `ResultConversionFilterTests` now builds that state from JSON; the DreamTravel OpenAPI contract
  snapshot gained `readOnly: true` on `isSuccess` and `data`.
- **04:** snippet verification is recorded in this record instead of a PR description, because the
  change was committed directly to `master`.
- **05:** `AggregateErrorTests` (3 tests) landed before the refactor to cover the empty branch.

## Follow-ups

- Tale step "Tale — `TaleJsonOptions.Default` registers the Tale error discriminators" (tale 02)
  extends the architecture page with the Tale discriminators.

## Post-completion review fixes (2026-11-03)

An independent code review of the completed feature found one issue; the fix is on `master`.

- **C1 — finding:** `Result` and `Result<T>` still had a compiler-generated public parameterless
  constructor, so `new Result()` (and the JSON `{}`) built a failure without an error, and
  `{"IsSuccess":true,"Error":{...}}` deserialized to a success that carries an error.
  **Fix:** both types declare `[JsonConstructor] protected Result() { }` (derivation stays
  possible), and `Result` implements `IJsonOnDeserialized`, throwing `JsonException` when
  `IsSuccess == (Error != null)`. The 1.x shapes still read (`RoundTrip_1xResultJson_Deserializes`
  and `Serialize_Result_MatchesTheShapeStoredBy1x` unchanged and green).
  **Tests:** `ResultTests.Constructors_AreNotPublic` (both types) and
  `ResultTests.Deserialize_InconsistentState_ThrowsJsonException` (`{}`, `{"IsSuccess":false}`,
  success with an error; `Result` and `Result<int>`) — 5 cases failed before the fix and pass after.
  **Test change:** `ResultConversionFilterTests.Failed_Result_Without_Error_Falls_Back_To_500_ProblemDetails`
  can no longer build its malformed result from `{"IsSuccess":false}`; it now uses a test-local
  record derived from `Result<Payload>`, which is the only remaining way to reach that state (this
  supersedes the step 02 deviation note). DreamTravel's UI reads `Result<T>` only from 2xx
  responses, so it is unaffected.
