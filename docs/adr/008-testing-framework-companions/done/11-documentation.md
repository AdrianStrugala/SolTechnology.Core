---
adr: 008-testing-framework-companions
step: 11 of 11
status: done
---

<!-- Reviewed: renumbered from to-do/09-documentation.md. Readme-stub references → steps 02–08. -->

# Step 11: Documentation + per-package readmes

## Summary
Document the framework as a coherent story and wire each package's `PackageReadmeFile`. Closes the
loop opened by the readme stubs in steps 02–08. Separate PR — pure documentation, no code.

## Affected components
- `docs/theQuality.md` — replace the inline `SQLFixture`/`IntegrationTestsFixture`/`WireMockFixture` code samples with the new companion packages; add a "Testing framework packages" section mapping concern → package; add a **"Container lifetime & reuse"** subsection (`TESTCONTAINERS_REUSE`, restart-if-stopped, readiness probes); keep the NUnit pyramid intact. Note the deliberate **no-automated-test-project / manual-smoke** policy for the `.Testing` packages.
- `docs/Testing.md` — readme for `SolTechnology.Core.Testing` (created as stub in step 02); document the AutoFixture (not Bogus) decision and the lifetime/reuse model.
- `docs/Sql.md` — add `SQL.Testing` companion section (MSSQL + Postgres, dacpac/EF/scripts provisioning, Respawn reset, the `ISharedSQLContainer` shared-MSSQL contract, `SQLFixture` all-caps type name). Note DacFx stays in `Sql` at runtime.
- `docs/Clients.md` / `docs/HTTP-Production-Checklist.md` — document `HTTP.Testing` mock DSL + the **breaking** `Faker` → `HTTP.Testing` migration note.
- `docs/Api.md` — document the new `API.Testing` auth-client + config-override helpers.
- `docs/Cache.md` (and/or new `docs/Redis.md`) — `Redis.Testing`.
- `docs/Blob.md` — `BlobStorage.Testing` (Azurite, Azure-specific).
- `docs/Bus.md` — `ServiceBus.Testing` (emulator + AMQP readiness + reuse + shared MSSQL catalog isolation).
- `docs/adr/README.md` — flip ADR-008 implementation status to ✅ Done once steps complete (left to `implement-plan`).

## Details
- Each package csproj `PackageReadmeFile` must resolve to a real `docs/*.md` — verify with `documentation-cleanup` skill.
- Cross-link ADR-008 from the testing docs.
- Include a migration table: old (`Faker`, in-`Sql` `SQLFixture`, local copies) → new package, calling out the **breaking** `Faker` namespace change explicitly.
- Document the AutoFixture-stays / Bogus-optional decision and the `TESTCONTAINERS_REUSE` workflow prominently.
- Document that the companions ship **without test projects**; verification is build + documented manual smoke.

## Acceptance criteria
- Every new/extended package has a resolving readme.
- `documentation-cleanup` passes (links, tables, module/doc parity).
- `theQuality.md` no longer references deleted/local fixtures and documents the reuse model and the no-test-project policy.

## Open questions
- New `docs/Redis.md` vs folding Redis testing into `docs/Cache.md`? Decide during implementation.

