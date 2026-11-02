---
spec: 2026-11-02-core-2-0-hangfire
step: 07
status: to-do
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
- [ ] `find src/SolTechnology.Core.Hangfire -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `grep -rnE "catch\s*\{\s*\}" src/SolTechnology.Core.Hangfire` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green.
- [ ] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.Hangfire --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [ ] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [ ] RM-7/RM-16: `git diff --stat -- tests/SolTechnology.Core.Hangfire.Tests/HangfirePersistedIdentityTests.cs` for this step prints nothing, and the test is green.

## Open questions
- none

## Deviations
