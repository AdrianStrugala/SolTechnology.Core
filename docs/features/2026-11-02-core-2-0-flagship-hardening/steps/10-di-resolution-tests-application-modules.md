---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 10
status: to-do
---

# Step 10: DI resolution tests — application modules

## Summary
Existing installer tests only check service descriptors, so they missed resolution failures
`B1`, `B2`, and `B4` (`P1-2`). This step adds one resolution test per application module. Each
test builds the provider with `ValidateOnBuild = true` and `ValidateScopes = true`, then resolves
every public service the module registers. Known-broken registrations land as `[Ignore]`d tests
that cite the finding ID, and the phase D step that fixes each one removes the `Ignore`.

## Affected components
- `tests/SolTechnology.Core.CQRS.Tests/ModuleResolutionTests.cs` — NEW
- `tests/SolTechnology.Core.Tale.Tests/ModuleResolutionTests.cs` — NEW
- `tests/SolTechnology.Core.Logging.Tests/ModuleResolutionTests.cs` — NEW
- `tests/SolTechnology.Core.HTTP.Tests/ModuleResolutionTests.cs` — NEW
- `tests/SolTechnology.Core.API.Tests/ModuleResolutionTests.cs` — NEW
- `tests/SolTechnology.Core.Authentication.Tests/ModuleResolutionTests.cs` — NEW

## Changes
- Pattern per test (NUnit, FluentAssertions, `// Arrange/Act/Assert`, name `AddSol<Module>_ResolvesAllPublicServices_WithValidation`):
  - `new ServiceCollection().AddLogging()`, then the module's `AddSol*` overloads.
  - `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })`.
  - Resolve every public service type inside `CreateScope()`.
- Coverage:
  - CQRS: `IMediator`, a test command/query/event handler.
  - Tale: `TaleManager`, `ITaleRepository`, a test handler.
  - Logging: both `AddSolLogging()` overloads, resolving `ITimingService`, `TimeProvider`, `ICorrelationIdService`, and `LoggingMiddleware` dependencies.
  - HTTP: a typed client from `AddSolHTTPClient`.
  - Api: the filters and `IProblemDetailsService`.
  - Authentication: `IAuthenticationSchemeProvider` with the `ApiKey` scheme.
- `AddSolLogging(IConfiguration)` gets `[Ignore("B4: ITimingService/TimeProvider not registered")]`.
- Follow the [`test-writing`](../../../../.github/skills/test-writing/SKILL.md) skill. No shared helper in a shipped package.

## Acceptance criteria
- [ ] `dotnet test` on each of the 6 projects is green, with exactly one ignored test (Logging, B4).
- [ ] Removing the `[Ignore]` locally reproduces the "Unable to resolve service for type 'ITimingService'" failure.

## Open questions
- none

## Deviations
