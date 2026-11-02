---
spec: 2026-11-02-core-2-0-core-result
step: 05
status: to-do
---

# Step 05: Guide conformance — Core

## Summary
`AggregateError.cs:37` has unbraced `if`s (`cqrs-tale#25`). This is a behaviour-preserving
refactor of the base package.

Former core step 66 (Core part) plus the Core-scoped part of former step 70; split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core/Errors/AggregateError.cs` — EDIT — braces

## Changes
- No public or protected signature change.
- Behaviour-preserving refactor; use the [`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.
- Log templates: the scoped `[{Placeholder}]` check below is this library's part of former step 70 (L-03). Fix any unbracketed placeholder it finds and add the property name to the core brief's L-03 entry.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Tests` and `dotnet test tests/SolTechnology.Core.CQRS.Tests` are green.
- [ ] `find src/SolTechnology.Core -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [ ] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).

## Open questions
- none

## Deviations
