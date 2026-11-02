---
spec: 2026-11-02-core-2-0-sql
step: 07
status: to-do
---

# Step 07: Guide conformance — SQL

## Summary
SQL hand-writes constructors, uses `static class` + `this`, and has unbraced `if`s and essay
comments (`infra#24`). This is a behaviour-preserving refactor.

Former core step 69 (SQL part) plus the SQL-scoped part of former step 70; split per library on 2026-11-02. The `SQL.Testing` part is testing-kits step 06.

## Affected components
- `src/SolTechnology.Core.SQL/**` (including `ModuleInstaller.cs`) — EDIT — `extension` blocks, braces, comments

## Changes
- No public or protected signature change; no log-template change.
- Behaviour-preserving refactor; use the [`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.
- Log templates: the scoped `[{Placeholder}]` check below is this library's part of former step 70 (L-03). Fix any unbracketed placeholder it finds and add the property name to the core brief's L-03 entry.

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.SQL -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `grep -rnE "catch\s*\{\s*\}" src/SolTechnology.Core.SQL` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.SQL.Tests` is green (Docker).
- [ ] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.SQL --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [ ] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations
