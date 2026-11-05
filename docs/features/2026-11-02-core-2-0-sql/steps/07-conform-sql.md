---
spec: 2026-11-02-core-2-0-sql
step: 07
status: done
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
- [x] `find src/SolTechnology.Core.SQL -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] `grep -rnE "catch\s*\{\s*\}" src/SolTechnology.Core.SQL` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.SQL.Tests` is green (Docker).
- [x] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.SQL --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [x] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations

### Scope of the conformance changes

**Original plan:** `extension` blocks, braces, and comments across `src/SolTechnology.Core.SQL/**`, after SQL "hand-writes
constructors, uses `static class` + `this`, and has unbraced `if`s and essay comments".

**Actual implementation:** steps 03 and 04 had already braced every `if` and left no hand-written constructor, so this
step changed three things. `ModuleInstaller.AddSolSQL` and `SqlHealthCheckExtensions.AddSolSqlHealthCheck` moved into
C# 14 `extension` blocks (`ModuleInstaller.cs` also became file-scoped and lost an unused `using`). The two-line comment
in `SqlHealthCheck` became one line, and the `// 3,9,27s` and `//delay up to 1s` comments that restated the retry
expression in `SQLConnectionFactory` were deleted. No file exceeds 80 lines. The premortem mitigation for
`ModuleInstaller.cs` (RM-13, step 03) stays in place: `ValidateOnStart()` still guards a non-empty `ConnectionString`
and `ArgumentNullException.ThrowIfNull` still guards the configuration, pinned by `ModuleInstallerTests`.

### Log-template check ran with perl as well

**Original plan:** the `grep -rnE` template check prints nothing.

**Actual implementation:** it prints nothing, but BSD `grep` does not match an unbracketed template such as
`"x {Foo} y"` either (as the cache feature found), so a `perl` check that flags any `{Placeholder}` not wrapped in `[]`
inside a string literal also ran and printed nothing; it does flag `"x {Foo} y"`. The SQL templates are the four from
step 04 (`[{DacpacPath}]`, `[{DatabaseName}]`, `[{DacStatus}] [{DacProgress}]`, `[{DacMessage}]`), so no property
name is added to the core brief's L-03 entry. SQL has no `[LoggerMessage]` templates.

### Public API diff

**Original plan:** the public API diff is empty.

**Actual implementation:** `Microsoft.DotNet.ApiCompat.Tool` 10.0.403 compared the Release `SolTechnology.Core.SQL.dll`
of `e3a8c3ac` with the refactored one: no breaking change with the attribute and parameter-name rules. Strict mode, in
both directions, lists only the two compiler-generated `extension` marker types
(`ModuleInstaller.<G>$…` and `SqlHealthCheckExtensions.<G>$…`), as in the CQRS feature.

