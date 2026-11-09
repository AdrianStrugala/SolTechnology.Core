---
spec: 2026-11-02-core-2-0-hangfire
step: 07
status: done
---

# Step 07: Guide conformance — Hangfire

## Summary
Hangfire hand-writes constructors, uses `static class` + `this`, and has unbraced `if`s and
essay comments (`infra#24`). This is a behaviour-preserving refactor that must not touch the
persisted job identity.

Former core step 69 (Hangfire part) plus the Hangfire-scoped part of former step 70; split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.Hangfire/**` (including `ModuleInstaller.cs`) — EDIT — `extension` blocks, braces, comments

## Changes
- No public or protected signature change; no log-template change.
- Behaviour-preserving refactor; use the [`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.
- Log templates: the scoped `[{Placeholder}]` check below is this library's part of former step 70 (L-03). Fix any unbracketed placeholder it finds and add the property name to the core brief's L-03 entry.
- Premortem RM-7 and RM-16 (core step 00, 2026-11-02, scenarios 7 and 16): Hangfire stores job type, assembly, method names, and parameter keys in every job. Guide conformance here must not rename `RecurringJobRunner<TJob>.RunAsync` (the Guide §10 `Async` rule does not apply to a persisted job method). It must not move `HangfireEventPublisher` or `RecurringJobRunner<TJob>` to another namespace, and it must not change the correlation job-parameter key. `HangfirePersistedIdentityTests` (step 03) and the step 04 registrar and retry tests keep their names and assertions.

## Acceptance criteria
- [x] `find src/SolTechnology.Core.Hangfire -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] `grep -rnE "catch\s*\{\s*\}" src/SolTechnology.Core.Hangfire` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green.
- [x] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.Hangfire --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [x] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [x] RM-7/RM-16: `git diff --stat -- tests/SolTechnology.Core.Hangfire.Tests/HangfirePersistedIdentityTests.cs` for this step prints nothing, and the test is green.

## Open questions
- none

## Deviations

### Only `ModuleInstaller` needed a change

**Original plan:** `extension` blocks, braces and comments across `src/SolTechnology.Core.Hangfire/**`.

**Actual implementation:** Steps 02–05 already wrote every touched file to the Guide (primary constructors, braces,
one-line why-comments, files under 150 lines, `[{Placeholder}]` templates), so the conformance pass changed only
`ModuleInstaller.cs`: `AddSolPersistentEvents` and `AddSolRecurringJob<TJob>` move into an
`extension(IServiceCollection services)` block and `UseSolFilters` into an `extension(IGlobalConfiguration
configuration)` block, and the two "// Filters" label comments become one private `AddSolJobFilters()` member with a
one-line why. No §9.7 exception applies (`TJob` is constrained to `class, IJob`). The library-scoped template check
finds nothing; a multi-line-safe variant (every string literal with a `{Placeholder}` in the library) lists only the
three bracketed templates added in steps 02 and 04, so the core brief's L-03 entry is unchanged (Hangfire had no
template at `4097d47f`).

Verification: ApiCompat old→new reports no breaking change; new→old reports only the two compiler-generated
`extension` marker types, as for Blob and MessageBus. `HangfirePersistedIdentityTests.cs` has no diff in this step.
`dotnet test tests/SolTechnology.Core.Hangfire.Tests -c Release` 61/61 green; `dotnet build SolTechnology.Core.slnx -c
Release` succeeds with warnings only in the untouched DreamTravel sample projects; `dotnet test SolTechnology.Core.slnx
-c Release` 901 passed, 11 skipped (the DreamTravel geolocation integration tests), 0 failed.
