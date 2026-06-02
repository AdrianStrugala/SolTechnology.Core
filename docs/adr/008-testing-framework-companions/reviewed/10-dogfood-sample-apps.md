---
adr: 008-testing-framework-companions
step: 10 of 11
status: reviewed
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

