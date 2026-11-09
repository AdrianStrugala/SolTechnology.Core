---
spec: 2026-11-02-core-2-0-hangfire
step: 01
status: done
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
- [x] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green. The only ignored test is infra#13.
- [x] The ignored test, run alone with its `Ignore` removed locally, fails because `ICorrelationIdService` is null.

## Open questions
- none

## Deviations

### Two tests ignored for infra#13, and `Hangfire.NetCore` in the test project

**Original plan:** One resolution test, ignored for infra#13; the test project gains only `Hangfire.InMemory`.

**Actual implementation:** `AddSolHangfire_ResolvesAllPublicServices_WithValidation` fails at
`BuildServiceProvider` under `ValidateOnBuild` ("Unable to resolve service for type
'ICorrelationIdService' while attempting to activate 'CorrelationIdJobFilter'"), so a second test,
`AddSolHangfire_WithoutAddSolLogging_RegistersCorrelationIdService`, builds the provider without
validation and shows the plan's symptom ("Expected object not to be <null>"). Both cite infra#13.
`services.AddHangfire(...)` lives in `Hangfire.NetCore`, so the test project also references it
(`1.8.23`, already central and canonical). `Hangfire.InMemory` was already in
`Directory.Packages.props` for DreamTravel; only the canonical-versions row is new.
