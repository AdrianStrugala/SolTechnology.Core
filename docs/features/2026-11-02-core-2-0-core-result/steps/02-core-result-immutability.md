---
spec: 2026-11-02-core-2-0-core-result
step: 02
status: to-do
---

# Step 02: Core — immutable `Result` and a typed `Result<T>.FailAsTask`

## Summary
`Result` exposes public `init` setters, which allow contradictory states such as
`{ IsSuccess = true, Error = x }`. `Result<T>` has no `FailAsTask(Error)`, so the inherited one
returns `Task<Result>` (`cqrs-tale#21`, `src/SolTechnology.Core/Result.cs:11,17,52,74`). 2.0 makes
the factories the only way to build a `Result` (`C-02`).

Former core step 38; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core/Result.cs` — EDIT — `private init` on `IsSuccess`, `Error`, `Data`; NEW `public static new Task<Result<T>> FailAsTask(Error error)` on `Result<T>`
- `src/**` and `sample-tale-code-apps/**` — EDIT — any object-initializer construction of `Result`/`Result<T>` (`grep -rnE "new Result(<[^>]+>)?\s*\{" src sample-tale-code-apps tests`)
- `tests/SolTechnology.Core.CQRS.Tests/Errors/ErrorSerializationTests.cs` — EDIT — `Result` JSON round-trip still deserializes (`[JsonConstructor]` or `[JsonInclude]` on private init)
- `tests/SolTechnology.Core.Tests/SolTechnology.Core.Tests.csproj` — EDIT — `ProjectReference` to `src/SolTechnology.Core/SolTechnology.Core.csproj` (the project holds only repository guard tests today and references no package; plan review 2026-11-02)
- `tests/SolTechnology.Core.Tests/ResultTests.cs` — NEW — factory invariants and `FailAsTask` type

## Changes
- Keep JSON shape identical. ProblemDetails and Tale persistence serialize `Result` (verify with a snapshot test). This is a persisted-contract touchpoint (premortem trigger): a `Result` stored by 1.x must still deserialize.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green, including DreamTravel.
- [ ] `dotnet test tests/SolTechnology.Core.Tests` and `dotnet test tests/SolTechnology.Core.CQRS.Tests` are green.
- [ ] `Result<int>.FailAsTask(new Error(...))` compiles to `Task<Result<int>>` (test).
- [ ] `RoundTrip_1xResultJson_Deserializes` (a JSON literal captured at `4097d47f`) is green.
- [ ] Premortem mitigations for the persisted contract are applied.

## Open questions
- none (`C-02` confirmed by the user on 2026-11-02)

## Deviations
