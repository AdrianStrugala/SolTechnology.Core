---
spec: 2026-11-02-core-2-0-hangfire
step: 01
status: to-do
---

# Step 01: DI resolution test — Hangfire

## Summary
Existing installer tests only check service descriptors, so they missed resolution failures `B1`, `B2`, and `B4` (`P1-2`). This step adds the Hangfire resolution test. It builds the provider with `ValidateOnBuild = true` and `ValidateScopes = true`, then resolves every public service the module registers. Known-broken registrations land as `[Ignore]`d tests that cite the finding ID, and the later step of this feature that fixes each one removes the `Ignore`.

Former core step 11 (Hangfire part); split per library on 2026-11-02.

## Affected components
- `tests/SolTechnology.Core.Hangfire.Tests/ModuleResolutionTests.cs` — NEW
- `tests/SolTechnology.Core.Hangfire.Tests/SolTechnology.Core.Hangfire.Tests.csproj` — EDIT — `PackageReference Hangfire.InMemory 1.0.0` (the version DreamTravel already uses; added to `canonical-versions.md`)
- `.github/skills/package-management/references/canonical-versions.md` — EDIT — NEW `Hangfire.InMemory` `1.0.0` row (test-only)

## Changes
- Pattern (NUnit, FluentAssertions, `// Arrange/Act/Assert`, name `AddSol<Module>_ResolvesAllPublicServices_WithValidation`):
  - `new ServiceCollection().AddLogging()`, then the module's `AddSol*` overloads.
  - `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })`.
  - Resolve every public service type inside `CreateScope()`.
- Dummy connection strings are enough because resolution must not open connections; record a deviation if one does.
- Coverage: `services.AddHangfire(c => c.UseInMemoryStorage())` so `IRecurringJobManager` and `RecurringJobRegistrar` resolve; then `AddSolRecurringJob<TestJob>` plus `AddSolPersistentEvents` without `AddSolLogging`.
- `[Ignore("infra#13 …")]` (`ICorrelationIdService` is not registered by Hangfire); step 02 removes it.
- Follow the [`test-writing`](../../../../.github/skills/test-writing/SKILL.md) skill. No shared helper in a shipped package.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green. The only ignored test is infra#13.
- [ ] The ignored test, run alone with its `Ignore` removed locally, fails because `ICorrelationIdService` is null.

## Open questions
- none

## Deviations
