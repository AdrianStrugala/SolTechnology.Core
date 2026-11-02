---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 17
status: to-do
---

# Step 17: Core — open `Error` polymorphism; `AggregateError` round-trip

## Summary
`Error` declares a closed `[JsonDerivedType]` list (`src/SolTechnology.Core/Error.cs:13-21`).
Serializing any other subtype, including `TaleCancelledError` and `TalePausedError`, throws
`NotSupportedException`. That breaks `TaleController`'s `BadRequest(Result.Fail(err))` and
`InMemoryTaleRepository.Clone()` (`B7`, `cqrs-tale#3`). Separately, `AggregateError.InnerErrors`
is lost on round-trip (`cqrs-tale#17`). This implements design A1(c) and changes a persisted JSON
contract (P-01).

The Tale-side discriminators only help where `TaleJsonOptions.Default` is used. Today
`TaleInstance.Clone()` (`Models/TaleInstance.cs:55-56`) and DreamTravel's
`SQLiteTaleRepository.cs:230-234` serialize `ChapterInfo.Error` (`ChapterInfo.cs:50`) with default
options, so after the fallback a `TalePausedError` would come back as a plain `Error`. RQ5 (user,
2026-11-02): `TaleJsonOptions` becomes public (additive API, `C-26`) and both call sites use it.

## Affected components
- `src/SolTechnology.Core/Error.cs` — EDIT — `[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]`
- `src/SolTechnology.Core/Errors/AggregateError.cs` — EDIT — `[JsonConstructor] AggregateError(string message, IReadOnlyList<Error> innerErrors)`
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — `:15` remove the internal `TaleJsonOptions` declaration; update usings at `:335,439`
- `src/SolTechnology.Core.Tale/Persistence/TaleJsonOptions.cs` — NEW — `public static class TaleJsonOptions` in `SolTechnology.Core.Tale.Persistence` (next to `ITaleRepository`, which custom repositories implement) with `public static JsonSerializerOptions Default { get; }`: today's settings plus a `TypeInfoResolver` modifier adding `taleCancelled` / `talePaused` derived types to `Error`; `MakeReadOnly(populateMissingResolver: true)` so consumers cannot mutate the shared instance (`C-26`)
- `src/SolTechnology.Core.Tale/Orchestration/TaleManager.cs` — EDIT — `:121` using update
- `src/SolTechnology.Core.Tale/Models/TaleInstance.cs` — EDIT — `:55-56` `Clone()` serializes and deserializes with `TaleJsonOptions.Default`
- `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite/SQLiteTaleRepository.cs` — EDIT — `:230-234` and the matching reads use `TaleJsonOptions.Default` for `History` and `CurrentChapter`
- `tests/SolTechnology.Core.CQRS.Tests/Errors/ErrorSerializationTests.cs` — EDIT — custom subtype and `InnerErrors.Count` (existing home of `RoundTrip_AggregateError_PreservesSubtype`)
- `tests/SolTechnology.Core.Tale.Tests/TypedErrorAndLifecycleTests.cs` — EDIT — Tale errors round-trip through `TaleJsonOptions.Default` (public, so no `InternalsVisibleTo` is needed)
- `tests/SolTechnology.Core.Tale.Tests/InMemoryRepositoryTests.cs` — EDIT — `Clone` preserves the error subtype

## Changes
- Keep existing discriminators (`error`, `notFound`, … `deadlock`) byte-identical.
- Tests:
  - `Serialize_UnknownSubtype_FallsBackToError`.
  - `RoundTrip_AggregateError_PreservesInnerErrors` (asserts `InnerErrors.Count` and the subtypes).
  - `RoundTrip_TalePausedError_WithTaleJsonOptions_PreservesType`.
  - `Clone_ChapterWithTalePausedError_PreservesErrorType`.
- `docs/Tale.md` tells custom-repository authors to serialize `TaleInstance` parts with `TaleJsonOptions.Default` (step 60).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.CQRS.Tests` and `dotnet test tests/SolTechnology.Core.Tale.Tests` are green.
- [ ] JSON of each pre-existing discriminator is unchanged (snapshot assertion in `ErrorSerializationTests`).
- [ ] No public or protected symbol renamed or removed; the only new public symbol is `TaleJsonOptions` (`C-26`, confirmed by the user on 2026-11-02, RQ5).
- [ ] `grep -rn "JsonSerializer\.\(Serialize\|Deserialize\)" src/SolTechnology.Core.Tale/Models/TaleInstance.cs sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite/SQLiteTaleRepository.cs` shows `TaleJsonOptions.Default` on every call.

## Open questions
- none (RQ5 resolved by the user on 2026-11-02: public `TaleJsonOptions`)

## Deviations
