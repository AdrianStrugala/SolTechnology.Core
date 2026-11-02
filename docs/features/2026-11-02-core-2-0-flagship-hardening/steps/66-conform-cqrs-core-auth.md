---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 66
status: to-do
---

# Step 66: Guide conformance — CQRS, Core, Authentication

## Summary
`Mediator`, `FluentValidationPipelineBehavior`, and `LoggingPipelineBehavior` hand-write
constructor fields. `CQRS/ModuleInstaller.cs:13` uses `static class` + `this`. `AggregateError.cs:37`
has unbraced `if`s, and some comments are stale MediatR references (`cqrs-tale#23,25`,
`logging-cache#26`). This is a behaviour-preserving refactor.

## Affected components
- `src/SolTechnology.Core.CQRS/Internal/Mediator.cs` — EDIT — `:11-18` primary constructor
- `src/SolTechnology.Core.CQRS/PipelineBehaviors/FluentValidationPipelineBehavior.cs` — EDIT — `:14-19`
- `src/SolTechnology.Core.CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs` — EDIT — `:16-21`
- `src/SolTechnology.Core.CQRS/ModuleInstaller.cs` — EDIT — `extension(IServiceCollection services)` block
- `src/SolTechnology.Core/Errors/AggregateError.cs` — EDIT — braces
- `src/SolTechnology.Core.CQRS/Nothing.cs` — EDIT — `:9` remove `//I'm just void`
- `src/SolTechnology.Core.Logging/LogScopeAttribute.cs` — EDIT — `:5` remove the MediatR comment
- `src/SolTechnology.Core.Authentication/ModuleInstaller.cs` — EDIT — `extension` block

## Changes
- No public or protected signature change.

## Acceptance criteria
- [ ] `grep -rn "this IServiceCollection" src/SolTechnology.Core.CQRS src/SolTechnology.Core.Authentication` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.CQRS.Tests` and `dotnet test tests/SolTechnology.Core.Authentication.Tests` are green.
- [ ] Public API diff is empty.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations
