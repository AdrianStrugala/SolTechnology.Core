---
spec: 2026-11-02-core-2-0-core-result
step: 01
status: to-do
---

# Step 01: Core — open `Error` polymorphism; `AggregateError` round-trip

## Summary
`Error` declares a closed `[JsonDerivedType]` list (`src/SolTechnology.Core/Error.cs:13-21`).
Serializing any other subtype, including `TaleCancelledError` and `TalePausedError`, throws
`NotSupportedException`. That breaks `TaleController`'s `BadRequest(Result.Fail(err))` and
`InMemoryTaleRepository.Clone()` (`B7`, `cqrs-tale#3`). Separately, `AggregateError.InnerErrors`
is lost on round-trip (`cqrs-tale#17`). This implements the Core half of design A1(c) and changes a
persisted JSON contract (P-01). The Tale half (public `TaleJsonOptions.Default` with the Tale
discriminators, `C-26`) is tale step 02; until it merges, Tale errors serialize as their nearest
ancestor instead of throwing.

Former core step 17 (Core part); split per library on 2026-11-02. The Tale part is tale step 02.

## Affected components
- `src/SolTechnology.Core/Error.cs` — EDIT — `[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]`
- `src/SolTechnology.Core/Errors/AggregateError.cs` — EDIT — `[JsonConstructor] AggregateError(string message, IReadOnlyList<Error> innerErrors)`
- `tests/SolTechnology.Core.CQRS.Tests/Errors/ErrorSerializationTests.cs` — EDIT — custom subtype and `InnerErrors.Count` (existing home of `RoundTrip_AggregateError_PreservesSubtype`)

## Changes
- Keep existing discriminators (`error`, `notFound`, … `deadlock`) byte-identical.
- Tests:
  - `Serialize_UnknownSubtype_FallsBackToError`.
  - `RoundTrip_AggregateError_PreservesInnerErrors` (asserts `InnerErrors.Count` and the subtypes).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.CQRS.Tests` is green.
- [ ] JSON of each pre-existing discriminator is unchanged (snapshot assertion in `ErrorSerializationTests`).
- [ ] No public or protected symbol renamed, removed, or added.

## Open questions
- none

## Deviations
