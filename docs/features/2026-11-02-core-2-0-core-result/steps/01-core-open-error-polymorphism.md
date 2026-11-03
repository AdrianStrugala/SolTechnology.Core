---
spec: 2026-11-02-core-2-0-core-result
step: 01
status: done
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
- [x] `dotnet test tests/SolTechnology.Core.CQRS.Tests` is green.
- [x] JSON of each pre-existing discriminator is unchanged (snapshot assertion in `ErrorSerializationTests`).
- [x] No public or protected symbol renamed, removed, or added.

## Open questions
- none

## Deviations

### Private JSON constructor for `AggregateError`

**Original plan:** add `[JsonConstructor] AggregateError(string message, IReadOnlyList<Error> innerErrors)`.

**Actual implementation:** the constructor is `private` (System.Text.Json accepts non-public `[JsonConstructor]` since .NET 8). A public `IReadOnlyList<Error>` overload would win overload resolution over the existing public `(string, IEnumerable<Error>)` constructor for every array or list argument and silently skip message building; private keeps the public surface unchanged, as the acceptance criteria require. It assigns the serialized `Message` as-is (it already contains the inner messages). `[JsonConstructor]` moved off the public parameterless constructor, which stays. Nine snapshot cases pin the JSON of every existing discriminator; they passed before and after the change. Full suite green.
