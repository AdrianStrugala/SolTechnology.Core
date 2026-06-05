---
adr: 008-testing-framework-companions
step: 10 of 11
status: done
---

<!-- Reviewed: renumbered from to-do/08-dogfood-sample-apps.md. This is the consumer-migration
     step referenced by step 04 (Faker breaking change). -->

<!-- PARTIAL BRING-FORWARD (after step 03, at maintainer request "make DreamTravel build"):
     DreamTravel.Component.Tests.csproj already gained `<ProjectReference>` to
     SolTechnology.Core.SQL.Testing (SQLFixture moved out of Sql) and bumped NSubstitute 4.0.0 → 5.3.0
     (NU1605 vs Core.Testing's 5.3.0). The Faker → HTTP.Testing migration for DreamTravel is still
     pending here (step 04 not yet done). TaleCode is untouched so far. -->

# Step 10: Dogfood — migrate in-repo sample apps, delete duplicates

## Summary
Prove the framework by migrating the two in-repo sample apps (TaleCode, DreamTravel) onto the new
companion packages and deleting the now-redundant local fixtures. This is the validation gate that
the packages actually replace the hand-rolled setup, **and the step that migrates consumers off the
breaking `Faker` rename** (step 04). Separate PR because it is consumer-side change, not package
authoring — never mixed with package plumbing.

## Affected components
- `sample-tale-code-apps/DreamTravel/tests/Component/` — replace local SQL/WireMock wiring with `SQL.Testing` + `HTTP.Testing`; keep `ComponentTestsFixture` orchestrator but compose from packages.
- `sample-tale-code-apps/TaleCode/tests/TaleCode.FunctionalTests/` and `TaleCode.IntegrationTests.*` — switch to new packages; remove `TaleCode.Faker` if fully superseded by `HTTP.Testing`.
- Update each app's test `.csproj` references: drop `SolTechnology.Core.Faker`, add `SolTechnology.Core.HTTP.Testing`; add `SolTechnology.Core.Testing`; keep `SolTechnology.Core.SQL.Testing` namespace working via the new package reference.
- Update `using` directives in test files from the old `SolTechnology.Core.Faker` namespace to `SolTechnology.Core.HTTP.Testing` (the rename is breaking — see step 04).
- Delete superseded local fixtures (duplicate `WireMockFixture`, app-local `SqlFixture`, local `Retry`).

## Details
- Type name `SQLFixture` and namespace `SolTechnology.Core.SQL.Testing` were preserved in step 03, so `SQLFixture` `using` directives stay; only the package reference changes.
- The `Faker` → `HTTP.Testing` namespace change **is** breaking; update call sites here. If step 04 chose the `[Obsolete]` shim, removing the `Faker` reference is still required to clear the obsolete warnings (`TreatWarningsAsErrors` is on).
- Validate the deterministic-publisher pattern (`SyncHangfireNotificationPublisher`) still works against the extended `API.Testing` config-override helper (step 05).
- **Validate the lifetime/reuse model end-to-end**: run the migrated suites twice with `TESTCONTAINERS_REUSE=true` and confirm the second run reuses containers and is measurably faster; confirm a clean run with reuse off still passes (hermetic mode). These are the sample apps' **own** existing test suites — no new `tests/SolTechnology.Core.*.Testing.Tests` projects are introduced, so the core CI lane is unaffected.
- `tests/tests-kyc` and `tests/tests-mts` are **out of scope** (external snapshots) — reference only, do not migrate.

## Acceptance criteria
- `dotnet test` for DreamTravel Component tests and TaleCode integration/functional tests passes on the new packages.
- No app retains a local copy of `WireMockFixture`, `SqlFixture`, or `Retry`.
- No remaining `PackageReference`/`ProjectReference` to `SolTechnology.Core.Faker` in sample apps.
- Second `TESTCONTAINERS_REUSE=true` run reuses containers (verified via container names / timing).

## Open questions
- Does TaleCode `TaleCode.Faker` contain app-specific fakes worth keeping app-side? Audit during implementation.

## Retrospective — Implementation Deviations

### 1. TaleCode is out of scope — no in-repo source to migrate
**Original plan:** migrate both in-repo sample apps (TaleCode + DreamTravel); audit whether
`TaleCode.Faker` holds app-specific fakes worth keeping (the open question).
**Actual implementation:** TaleCode has **zero tracked source** in this repo — `find sample-tale-code-apps/TaleCode -name '*.cs' -not -path '*/obj/*'` returns 0 files, `git ls-files sample-tale-code-apps/TaleCode` is empty, and no `*.csproj` exists outside `obj/`. Only stale `obj/` build
artifacts remain (incl. `TaleCode.Faker/obj`). TaleCode is not in `SolTechnology.Core.slnx` and cannot be
built or migrated. The open question is therefore **moot** (no source to audit). Only **DreamTravel** — the
one tracked, buildable in-repo sample — was migrated, which also matches the user's explicit scope.

### 2. DreamTravel package migration was already in place; only the `Retry` duplicate remained
**Original plan:** replace local SQL/WireMock wiring, swap `Faker` → `HTTP.Testing`, add `Core.Testing`,
delete duplicate `WireMockFixture` / `SqlFixture` / `Retry`.
**Actual implementation:** the package-reference + namespace migration had already landed in DreamTravel
via the partial bring-forward (steps 03/04/05): `DreamTravel.Component.Tests` already referenced
`API.Testing`, `SQL.Testing`, `HTTP.Testing`, used `SQLFixture` / `WireMockFixture` / `FakeApiBase` from the
packages, and carried **no** `SolTechnology.Core.Faker` reference. The only residual duplicate was a local
`public static class Retry` (method `Unless<T>`) at the bottom of `Trips/FindCityAndSaveDetailsTest.cs`.
Work done in this step:
- Deleted the local `Retry`; switched the call site to `SolTechnology.Core.Testing.Retry.UntilConditionMetOrTimeout`
  (signature-compatible: `action, condition, totalWaitTime, pauseInterval` — behaviour-preserving for the
  not-found→null assertion).
- Added an **explicit** `ProjectReference` to `SolTechnology.Core.Testing` (the foundation was only
  transitive before; `Retry` is now used directly) and removed the now-unused `using System.Diagnostics`.
- Removed the orphaned `src/SolTechnology.Core.Faker/` directory (only stale `obj/`, untracked, not in the
  slnx — the project itself was deleted in step 04).

### 3. Acceptance criteria — evidence
All runs: **5/5 passed**. Timings below are **process wall-clock** (`/usr/bin/time -p` `real`) so they are
comparable across modes — this is the apples-to-apples figure:

| Mode | Wall-clock (`real`) |
|---|---|
| Reuse OFF (hermetic, always cold) | **46.7 s** |
| Reuse ON, 1st run (cold, creates named containers) | 28.6 s |
| Reuse ON, 2nd run (warm reuse, skips re-provision) | **11.0 s** |

Warm reuse is **~4.2× faster** than the hermetic cold path (46.7 s → 11.0 s), satisfying the reuse-timing
criterion. The hermetic run still passes cleanly with reuse off.

> **Measurement caveat (corrects an earlier draft):** the VSTest summary line reports `Duration: 2–3 s` in
> *every* mode, because it measures only test-method execution — **not** container provisioning (SQL start +
> dacpac deploy + WireMock + two API hosts), which happens in `[OneTimeSetUp]` and dominates wall-clock.
> An earlier note here put the VSTest "3 s" next to the `/usr/bin/time` "28.6/11.0 s" figures, which mixed
> two different clocks and looked contradictory. The table above uses wall-clock throughout.

`TaleCode` criteria are N/A per deviation 1.

