---
spec: 2026-11-02-core-2-0-cqrs
step: 06
status: done
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
- [x] `grep -rn "this IServiceCollection" src/SolTechnology.Core.CQRS` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.CQRS.Tests` is green.
- [x] `find src/SolTechnology.Core.CQRS -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.CQRS --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [x] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations

### Verification methods and scope additions

**Original plan:** primary constructors in `Mediator`, `FluentValidationPipelineBehavior` and
`LoggingPipelineBehavior`; an `extension` block in `ModuleInstaller.cs`; remove the `Nothing.cs`
comment; tune the placeholder regex in the PR; public API diff via ref assemblies or ApiCompat;
apply the premortem mitigations for `ModuleInstaller.cs`.

**Actual implementation:** delivered in three commits (`refactor` primary constructors, `refactor`
`extension` block, `chore` comment). The internal `RequestDispatcher` (in
`Internal/RequestDispatcherCache.cs`) had the same hand-written capture and got a primary
constructor too. The public behavior constructors keep their parameter types and names; their XML
docs moved to `<param>` tags on the types. The given placeholder regex cannot see `[LoggerMessage]`
templates (the `Message =` line has no `Log…(`), so it was also run as
`grep -rnE "\"[^\"]*[^\[]\{[@A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.CQRS --include=*.cs | grep -v "///"`;
both print nothing, so the core brief's L-03 entry needs no CQRS property. `Microsoft.DotNet.ApiCompat.Tool`
10.0.403 (installed to a scratch tool path, then deleted) compared the Release
`SolTechnology.Core.CQRS.dll` before the step (`b9e0a57f`) and after it, and also the pre-feature
build (`effe7fa4`) with the final one: no breaking changes with the attribute and parameter-name
rules enabled; strict mode lists only the compiler-generated `ModuleInstaller.<G>$…` extension
marker type, which consumers cannot reference. The premortem mitigation that applies to this
installer edit is RM-16 (refactors must not rewrite pinned tests): `git diff --stat b9e0a57f HEAD -- tests/`
is empty, and the step 01 resolution test still passes. The primary-constructor commit (`dafd2001`)
was committed after the CQRS tests only; the full solution suite ran green before the next commit,
which includes it.
