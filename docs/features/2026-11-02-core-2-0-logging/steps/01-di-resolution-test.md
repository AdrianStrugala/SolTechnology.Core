---
spec: 2026-11-02-core-2-0-logging
step: 01
status: done
---

# Step 01: DI resolution test — Logging

## Summary
Existing installer tests only check service descriptors, so they missed resolution failures `B1`, `B2`, and `B4` (`P1-2`). This step adds the Logging resolution test. It builds the provider with `ValidateOnBuild = true` and `ValidateScopes = true`, then resolves every public service the module registers. Known-broken registrations land as `[Ignore]`d tests that cite the finding ID, and the later step of this feature that fixes each one removes the `Ignore`.

Former core step 10 (Logging part); split per library on 2026-11-02.

## Affected components
- `tests/SolTechnology.Core.Logging.Tests/ModuleResolutionTests.cs` — NEW

## Changes
- Pattern (NUnit, FluentAssertions, `// Arrange/Act/Assert`, name `AddSol<Module>_ResolvesAllPublicServices_WithValidation`):
  - `new ServiceCollection().AddLogging()`, then the module's `AddSol*` overloads.
  - `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })`.
  - Resolve every public service type inside `CreateScope()`.
- Coverage: both `AddSolLogging()` overloads, resolving `ITimingService`, `TimeProvider`, `ICorrelationIdService`, and `LoggingMiddleware` dependencies.
- `AddSolLogging(IConfiguration)` gets `[Ignore("B4: ITimingService/TimeProvider not registered")]`; step 02 removes it.
- Follow the [`test-writing`](../../../../.github/skills/test-writing/SKILL.md) skill. No shared helper in a shipped package.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green, with exactly one ignored test (B4).
- [x] Removing the `[Ignore]` locally reproduces the "Unable to resolve service for type 'ITimingService'" failure.

## Open questions
- none

## Deviations
