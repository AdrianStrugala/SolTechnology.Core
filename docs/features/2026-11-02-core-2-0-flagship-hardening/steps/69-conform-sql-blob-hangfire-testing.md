---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 69
status: to-do
---

# Step 69: Guide conformance — SQL, Blob, Hangfire, and the remaining testing kits

## Summary
The remaining infrastructure modules have the same Guide violations (`infra#24`,
`logging-cache#26`): `SQL.Testing/SQLFixture.cs` exceeds the cap (159), and SQL, Blob, Hangfire,
and the testing kits hand-write constructors, use `static class` + `this`, and have unbraced
`if`s and essay comments. `Guards.Tests` uses xUnit; Guards is reference-only and out of scope
(Q1, RQ4, accepted risk AR-2).

## Affected components
- `src/SolTechnology.Core.SQL.Testing/SQLFixture.cs` — EDIT — below 150 lines (extract engine-specific container setup into an internal file, NEW)
- `src/SolTechnology.Core.Blob/Connection/BlobConnectionFactory.cs` — EDIT — `:12-15` primary constructor
- `src/SolTechnology.Core.SQL/**`, `src/SolTechnology.Core.Hangfire/**` (including both `ModuleInstaller.cs` files) — EDIT — `extension` blocks, braces, comments
- `src/SolTechnology.Core.Testing/Containers/ContainerLifecycleHelper.cs` (`:15-19`), `src/SolTechnology.Core.Testing/Assertions/ResultAssertions.cs` (`:74`) — EDIT — `extension` blocks, braces, comments

## Changes
- No public or protected signature change; no log-template change.
- Premortem RM-7 and RM-16 (step 00, 2026-11-02, scenarios 7 and 16): Hangfire stores job type, assembly, method names, and parameter keys in every job. Guide conformance here must not rename `RecurringJobRunner<TJob>.RunAsync` (the Guide §10 `Async` rule does not apply to a persisted job method). It must not move `HangfireEventPublisher` or `RecurringJobRunner<TJob>` to another namespace, and it must not change the correlation job-parameter key. `HangfirePersistedIdentityTests` (step 32) and the step 51 registrar and retry tests keep their names and assertions.

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.SQL src/SolTechnology.Core.Blob src/SolTechnology.Core.Hangfire src/SolTechnology.Core.Testing src/SolTechnology.Core.SQL.Testing src/SolTechnology.Core.Redis.Testing src/SolTechnology.Core.Blob.Testing -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `find src -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing (whole-repository check after phase H, matching the brief).
- [ ] `grep -rnE "catch\s*\{\s*\}" src` prints nothing.
- [ ] `pwsh ./.github/runTests.ps1` is green (Docker).
- [ ] Public API diff is empty.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [ ] RM-7/RM-16: `git diff --stat -- tests/SolTechnology.Core.Hangfire.Tests/HangfirePersistedIdentityTests.cs` for this step prints nothing, and the test is green.

## Open questions
- none

## Deviations
