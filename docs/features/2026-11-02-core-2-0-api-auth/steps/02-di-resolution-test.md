---
spec: 2026-11-02-core-2-0-api-auth
step: 02
status: to-do
---

# Step 02: DI resolution test — Api and Auth

## Summary
Existing installer tests only check service descriptors, so they missed resolution failures `B1`, `B2`, and `B4` (`P1-2`). This step adds the Api and Auth resolution test. It builds the provider with `ValidateOnBuild = true` and `ValidateScopes = true`, then resolves every public service the module registers. Known-broken registrations land as `[Ignore]`d tests that cite the finding ID, and the later step of this feature that fixes each one removes the `Ignore`. Api and Auth ship as two packages; both tests land in one PR.

Former core step 10 (Api and Authentication parts); split per library on 2026-11-02. Renumbered from 01 on 2026-11-03 (step 01 renames `SolTechnology.Core.Authentication` to `SolTechnology.Core.Auth`, `C-28`).

## Affected components
- `tests/SolTechnology.Core.API.Tests/ModuleResolutionTests.cs` — NEW
- `tests/SolTechnology.Core.Auth.Tests/ModuleResolutionTests.cs` — NEW

## Changes
- Pattern (NUnit, FluentAssertions, `// Arrange/Act/Assert`, name `AddSol<Module>_ResolvesAllPublicServices_WithValidation`):
  - `new ServiceCollection().AddLogging()`, then the module's `AddSol*` overloads.
  - `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })`.
  - Resolve every public service type inside `CreateScope()`.
- Coverage: Api — the filters and `IProblemDetailsService`; Auth — `AddSolAuth(AuthConfiguration)`, then `IAuthenticationSchemeProvider` with the `ApiKey` scheme.
- Follow the [`test-writing`](../../../../.github/skills/test-writing/SKILL.md) skill. No shared helper in a shipped package.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.API.Tests` and `dotnet test tests/SolTechnology.Core.Auth.Tests` are green, with no ignored resolution test.

## Open questions
- none

## Deviations
