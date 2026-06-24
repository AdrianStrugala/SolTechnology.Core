---
adr: 012-production-pattern-adoption-wave-2
step: 05 of 24
status: reviewed
---

# Step 05: A2.2 — Medallion.Threading Postgres + SqlServer backends (`Core.DistributedLock`)

## Summary
Add the production lock backends to `SolTechnology.Core.DistributedLock`: Postgres advisory locks
and SqlServer application locks via the `DistributedLock.*` (Medallion.Threading) library, selectable
through `DistributedLockOptions`. Separate PR from the abstraction (step 04) because it introduces
**new third-party NuGet dependencies** that need a `package-management` pin + CVE gate.

## Affected components
- `src/SolTechnology.Core.DistributedLock/SolTechnology.Core.DistributedLock.csproj` — add
  `PackageReference`s: `DistributedLock.Postgres`, `DistributedLock.SqlServer`
  (and `DistributedLock.FileSystem` if the local backend is moved here from step 04).
- `src/SolTechnology.Core.DistributedLock/Postgres/PostgresDistributedLockService.cs` — Postgres
  advisory-lock backend.
- `src/SolTechnology.Core.DistributedLock/SqlServer/SqlServerDistributedLockService.cs` — SqlServer
  app-lock backend.
- `src/SolTechnology.Core.DistributedLock/DistributedLockOptions.cs` — add the **local
  connection-string accessor** (see the connection-source decision below).
- `src/SolTechnology.Core.DistributedLock/ModuleInstaller.cs` — extend backend selection
  (`AddDistributedLock` resolves Postgres / SqlServer / file from `DistributedLockOptions`).
- `.github/skills/package-management/references/canonical-versions.md` — add a row for each new
  `DistributedLock.*` package with the pinned version and the introducing project.
- `tests/SolTechnology.Core.DistributedLock.Tests/` — backend behaviour tests (use the
  `Core.Testing` Testcontainers fixtures for Postgres/SqlServer where an integration test is
  warranted; otherwise unit-test the namespacing + degrade-to-null contract).

## Details
- **Versions:** pin via the `package-management` skill — record the exact pinned versions in
  `canonical-versions.md` in the same PR. CVE check at 2026-06-24 returned **no known CVEs** for the
  `DistributedLock.*` family; re-run `validate_cves` on the exact pinned versions before merge
  (NuGet IDs `DistributedLock.Postgres` / `DistributedLock.SqlServer` / `DistributedLock.FileSystem`;
  the public namespace is `Medallion.Threading.*`).
- **Backends honour the same contract** as step 04: `TryAcquireLockAsync` returns a disposable on
  success, `null` on failure; a backend/timeout failure logs + returns `null` and never throws into
  the host loop. Map Medallion's acquire-timeout (`null` handle) to the same `null` semantics.
- **Connection source — DECISION (resolved): use a local options-based accessor.** The backends need
  a connection string / data source. Source it from `DistributedLockOptions` (a connection-string /
  `DbDataSource` accessor on the options) — **do NOT reuse `Core.SQL`'s `ISqlConnectionStringProvider`
  (ADR-010 S1).** Reusing it would force `Core.DistributedLock → Core.SQL`, dragging Dapper, Polly and
  `Microsoft.Data.SqlClient` into the otherwise dependency-light lock package and coupling it to the
  SQL module. A local accessor keeps the lock package independent and lets a host point it at any
  Postgres/SqlServer connection (which need not be the same store `Core.SQL` uses). Document the
  options shape.
- **Key namespacing** continues to apply — the Medallion lock name is `{prefix}/{name}`.
- Keep `Core.DistributedLock` free of hosting concerns — the leader-elected poller (step 10) is a
  separate consumer in `Core.Scheduler`.

## Acceptance criteria
- Postgres and SqlServer backends acquire/release through `DistributedLock.*` and honour the
  degrade-to-`null` guard-rail.
- The connection string / data source is resolved from `DistributedLockOptions` (local accessor);
  the package takes **no** reference on `Core.SQL`.
- `AddDistributedLock` selects the backend from `DistributedLockOptions`; misconfiguration fails at
  `ValidateOnStart`.
- `canonical-versions.md` carries a row per new `DistributedLock.*` package; `validate_cves` is
  re-run on the pinned versions and returns clean (or any finding is resolved per `dependency-audit`).
- Backend tests cover the success handle, the not-acquired `null`, and the no-throw-on-failure
  contract.

## Open questions
- none — the connection-source question is resolved above (local options accessor; no `Core.SQL`
  dependency).

