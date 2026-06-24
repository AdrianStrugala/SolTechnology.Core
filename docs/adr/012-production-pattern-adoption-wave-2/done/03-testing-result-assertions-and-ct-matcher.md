---
adr: 012-production-pattern-adoption-wave-2
step: 03 of 24
status: done
---

# Step 03: D1 + D2 — `Result` assertion helpers + `Ct` matcher alias (`Core.Testing`)

## Summary
Add two small test-authoring helpers to `SolTechnology.Core.Testing`: (D1) `Result`/`Result<T>`
assertions that surface `Error.Code + Message` on failure instead of a bare "expected true but was
false", and (D2) an NSubstitute-flavoured `Ct` alias for "any `CancellationToken`" to cut visual
noise in mock setups. Grouped into one PR because both are tiny, cohesive "reduce test friction"
helpers in the same module with no other dependencies.

## Affected components
- `src/SolTechnology.Core.Testing/Assertions/ResultAssertions.cs` — new `ShouldBeSuccess()` /
  `ShouldBeFailure()` extensions on `Result` and `Result<T>`.
- `src/SolTechnology.Core.Testing/Substitutes/Ct.cs` — new `Ct` helper exposing the
  "any cancellation token" argument matcher.
- `src/SolTechnology.Core.Testing/SolTechnology.Core.Testing.csproj` — **add**
  `<PackageReference Include="FluentAssertions" Version="7.2.2" />`. This is the **first
  FluentAssertions reference under `src/`** (FA otherwise lives only in `tests/Directory.Build.props`);
  it is accepted here because `Core.Testing` is the shipped **test-only companion** package, not a
  runtime/production assembly. **NSubstitute `5.3.0` is already referenced** in this csproj (used by
  the AutoFixture customisations) — the `Ct` matcher reuses it, **no new NSubstitute reference is
  needed**.
- `tests/SolTechnology.Core.Testing.Tests/` — **new** NUnit test project (none exists today) that
  self-tests both helpers. Wire it into `SolTechnology.Core.slnx` under the `/Tests/` folder. (The
  CLAUDE.md §1 new-top-level-test-folder confirmation is **GIVEN** by the maintainer for this wave.)

## Details
- **D1 — `Result` assertions.** Implement against Core's own `Result`/`Result<T>` and `Error`
  (`src/SolTechnology.Core/`), and the repo's chosen assertion lib **FluentAssertions 7.2.2**
  (per `package-management` canonical versions — NOT the app's Shouldly/CSharpFunctionalExtensions).
  - `ShouldBeSuccess()` on a failed result must throw with a message containing the failing
    `Error.Code` and `Error.Message` (and, where useful, a serialised view of the error/state) so
    the failure is self-explaining.
  - Provide `ShouldBeSuccess()` for `Result<T>` that returns the unwrapped `T` for fluent chaining.
  - Provide `ShouldBeFailure()` / `ShouldBeFailure<TError>()` returning the `Error` for further
    assertions.
  - Confirm the exact failure-shape members on Core's `Result`/`Error` (e.g. `IsSuccess`, `Error`,
    `Error.Message`) before writing — read `src/SolTechnology.Core/Result*.cs` and `Error.cs`.
- **D2 — `Ct` matcher.** NSubstitute is the repo mocking lib (Moq is anti-stack). Expose a concise
  alias for `Arg.Any<CancellationToken>()` — e.g. `Ct.Any` (a property returning the matcher) or
  `Any.Ct`. Pick one shape, document it, and keep it NSubstitute-native (no Moq `It.IsAny`).
- Both helpers are pure library additions to `Core.Testing` — no new package, no DI.

## Acceptance criteria
- `result.ShouldBeSuccess()` on a failure throws a message containing the `Error.Code` and
  `Error.Message`; on success it passes (and returns `T` for `Result<T>`).
- `ShouldBeFailure()` returns the `Error` for chaining.
- The `Ct` matcher substitutes cleanly for `Arg.Any<CancellationToken>()` in an NSubstitute setup.
- `SolTechnology.Core.Testing.csproj` references FluentAssertions `7.2.2` (new) and NSubstitute
  `5.3.0` (already present); no Shouldly / CSharpFunctionalExtensions / Moq added.
- The new `tests/SolTechnology.Core.Testing.Tests` project is in `SolTechnology.Core.slnx` (`/Tests/`)
  and its self-tests demonstrate both the passing and failing assertion messages.

## Open questions
- Final naming: `Ct.Any` vs `Any.Ct`. Recommend `Ct.Any` (discoverable via the `Ct` type); confirm
  with the reviewer.

