---
spec: 2026-11-02-core-2-0-tale
step: 02
status: done
---

# Step 02: Tale — public `TaleJsonOptions.Default` with Tale error discriminators

## Summary
After result step 01, `Error` falls back to its nearest ancestor for unknown subtypes, so a
`TalePausedError` or `TaleCancelledError` would come back as a plain `Error`. The Tale-side
discriminators only help where `TaleJsonOptions.Default` is used. Today `TaleInstance.Clone()`
(`Models/TaleInstance.cs:55-56`) and DreamTravel's `SQLiteTaleRepository.cs:230-234` serialize
`ChapterInfo.Error` (`ChapterInfo.cs:50`) with default options. RQ5 (user, 2026-11-02):
`TaleJsonOptions` becomes public (additive API, `C-26`) and both call sites use it. This is the Tale
half of design A1(c) (`B7`, P-01).

Former core step 17 (Tale part); split per library on 2026-11-02. The Core part is result step 01.

## Affected components
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — `:15` remove the internal `TaleJsonOptions` declaration; update usings at `:335,439`
- `src/SolTechnology.Core.Tale/Persistence/TaleJsonOptions.cs` — NEW — `public static class TaleJsonOptions` in `SolTechnology.Core.Tale.Persistence` (next to `ITaleRepository`, which custom repositories implement) with `public static JsonSerializerOptions Default { get; }`: today's settings plus a `TypeInfoResolver` modifier adding `taleCancelled` / `talePaused` derived types to `Error`; `MakeReadOnly(populateMissingResolver: true)` so consumers cannot mutate the shared instance (`C-26`)
- `src/SolTechnology.Core.Tale/Orchestration/TaleManager.cs` — EDIT — `:121` using update
- `src/SolTechnology.Core.Tale/Models/TaleInstance.cs` — EDIT — `:55-56` `Clone()` serializes and deserializes with `TaleJsonOptions.Default`
- `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite/SQLiteTaleRepository.cs` — EDIT — `:230-234` and the matching reads use `TaleJsonOptions.Default` for `History` and `CurrentChapter`
- `tests/SolTechnology.Core.Tale.Tests/TypedErrorAndLifecycleTests.cs` — EDIT — Tale errors round-trip through `TaleJsonOptions.Default` (public, so no `InternalsVisibleTo` is needed)
- `tests/SolTechnology.Core.Tale.Tests/InMemoryRepositoryTests.cs` — EDIT — `Clone` preserves the error subtype

## Changes
- Prerequisite: result step 01 is merged (nearest-ancestor fallback on `Error`).
- Tests:
  - `RoundTrip_TalePausedError_WithTaleJsonOptions_PreservesType`.
  - `Clone_ChapterWithTalePausedError_PreservesErrorType`.
- `docs/Tale.md` tells custom-repository authors to serialize `TaleInstance` parts with `TaleJsonOptions.Default` (step 13).

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green.
- [x] No public or protected symbol renamed or removed; the only new public symbol is `TaleJsonOptions` (`C-26`, confirmed by the user on 2026-11-02, RQ5).
- [x] `grep -rn "JsonSerializer\.\(Serialize\|Deserialize\)" src/SolTechnology.Core.Tale/Models/TaleInstance.cs sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite/SQLiteTaleRepository.cs` shows `TaleJsonOptions.Default` on every call.

## Open questions
- none (RQ5 resolved by the user on 2026-11-02: public `TaleJsonOptions`)

## Deviations
