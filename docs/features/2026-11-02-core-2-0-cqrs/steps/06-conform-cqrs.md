---
spec: 2026-11-02-core-2-0-cqrs
step: 06
status: to-do
---

# Step 06: Guide conformance — CQRS

## Summary
`Mediator`, `FluentValidationPipelineBehavior`, and `LoggingPipelineBehavior` hand-write
constructor fields. `CQRS/ModuleInstaller.cs:13` uses `static class` + `this`, and some comments
are stale (`cqrs-tale#23,25`, `logging-cache#26`). This is a behaviour-preserving refactor.

Former core step 66 (CQRS part) plus the CQRS-scoped part of former step 70; split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.CQRS/Internal/Mediator.cs` — EDIT — `:11-18` primary constructor
- `src/SolTechnology.Core.CQRS/PipelineBehaviors/FluentValidationPipelineBehavior.cs` — EDIT — `:14-19`
- `src/SolTechnology.Core.CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs` — EDIT — `:16-21`
- `src/SolTechnology.Core.CQRS/ModuleInstaller.cs` — EDIT — `extension(IServiceCollection services)` block
- `src/SolTechnology.Core.CQRS/Nothing.cs` — EDIT — `:9` remove `//I'm just void`

## Changes
- No public or protected signature change.
- Behaviour-preserving refactor; use the [`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.
- Log templates: the scoped `[{Placeholder}]` check below is this library's part of former step 70 (L-03). Fix any unbracketed placeholder it finds and add the property name to the core brief's L-03 entry.

## Acceptance criteria
- [ ] `grep -rn "this IServiceCollection" src/SolTechnology.Core.CQRS` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.CQRS.Tests` is green.
- [ ] `find src/SolTechnology.Core.CQRS -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.CQRS --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [ ] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations
