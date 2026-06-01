---
adr: 008-testing-framework-companions
step: 01 of 11
status: reviewed
---

<!-- Reviewed: renumbered from to-do/10-run-premortem.md. Premortem evaluates THE PLAN,
     so it is the gate between planning and implementation and now runs first. -->

# Step 01: Run premortem

## Summary
First gate before any production code is written. The premortem evaluates **the plan**, so it runs
**before** implementation begins. Implementation of ADR-008 is **blocked** until the
[premortem](../../../../.github/skills/premortem/SKILL.md) skill returns *Go* or
*Go with mitigations*.

## Affected components
- none (process gate)

## Details
- Run the premortem skill against the full plan, focusing on the highest-risk seams:
  - Extracting `SQLFixture` from `SolTechnology.Core.Sql` without breaking TaleCode/DreamTravel (namespace `SolTechnology.Core.SQL.Testing` preserved, dropped Testcontainers runtime dep; DacFx stays in `Sql` and is also added to `SQL.Testing`).
  - `Faker` → `HTTP.Testing` rename as an **accepted breaking change**: namespace changes, so type-forwarding is impossible; the migration path is a thin `[Obsolete]` shim or outright deletion of the `Faker` package after consumers migrate (step 10). Stranded external consumers must be covered by a migration note.
  - Respawn reset correctness across MSSQL **and** Postgres.
  - **Container lifetime/reuse**: `TESTCONTAINERS_REUSE` leaving stale/poisoned containers between runs; reuse interacting badly with schema provisioning (re-provision vs skip); Ryuk-disabled leaks; restart-if-stopped races.
  - **Service Bus emulator**: AMQP readiness-probe regressions (`NullReferenceException` in `AmqpTransportInitiator`), the unstable Testcontainers reuse-hash pitfall, and the shared-MSSQL contract exposed by `SQL.Testing` (isolated emulator catalog; Respawn scoped to the application catalog only).
  - Testcontainers/Docker availability — note that **no integration test projects are added to `tests/`**, so PR builds are unaffected; smoke checks are manual/documented only.
  - Seven packages: version/release coordination, the publish-workflow wiring (step 09), and inter-package version drift.
- Also recommended: run [blue-red-team](../../../../.github/skills/blue-red-team/SKILL.md) on the
  ORM-agnostic `SQL.Testing` decision (vs the rejected `EF.Testing`), the NUnit-only stance, the
  AutoFixture-stays (not Bogus) decision, and the **no-test-projects** decision for the `.Testing`
  packages.
- Record outcome and any mitigations; only then start step 02.

## Acceptance criteria
- Premortem verdict recorded as *Go* or *Go with mitigations*.
- Any mitigations folded back into the relevant step files before coding begins.

## Open questions
- none

