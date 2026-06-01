---
adr: 008-testing-framework-companions
step: 03 of 11
status: reviewed
---

<!-- Reviewed: renumbered from to-do/02-sql-testing-extract.md. Type stays `SQLFixture`
     (ADR-001 all-caps); dropped Testcontainers.MsSql; added shared-MSSQL contract. -->

# Step 03: Extract `SolTechnology.Core.SQL.Testing` (MSSQL + Postgres, ORM-agnostic)

## Summary
Move `SQLFixture` out of `SolTechnology.Core.Sql` into a dedicated companion package (capital `SQL`)
and generalise it to both MSSQL and Postgres with pluggable schema provisioning and Respawn-based
reset. Single PR because the engine abstraction, the fixture and the package move are one cohesive
unit (the options seam ships with the fixture that consumes it — never split).

## Affected components
- `src/SolTechnology.Core.SQL.Testing/SolTechnology.Core.SQL.Testing.csproj` — new package, `PackageId` `SolTechnology.Core.SQL.Testing` (Testcontainers, Testcontainers.PostgreSql, **Microsoft.SqlServer.DacFx**, Npgsql, Microsoft.Data.SqlClient, Respawn). **No `Testcontainers.MsSql`** — MSSQL uses the generic `ContainerBuilder` + host-side login probe (see `MsSqlEngine`). Depends on `SolTechnology.Core.Testing`. Version `0.1.0`.
- `src/SolTechnology.Core.SQL.Testing/SQLFixture.cs` — moved from `src/SolTechnology.Core.Sql/Testing/SQLFixture.cs`. **Preserve type name `SQLFixture` (all caps, per ADR-001) and namespace `SolTechnology.Core.SQL.Testing`** to avoid churn in TaleCode/DreamTravel.
- `src/SolTechnology.Core.SQL.Testing/ISharedSqlContainer.cs` — **public contract** exposing the running MSSQL container + shared docker network so `ServiceBus.Testing` (step 08) can consume the same MSSQL instance instead of spawning a second. (Resolves the previously-deferred open question.)
- `src/SolTechnology.Core.SQL.Testing/Engines/IDatabaseEngine.cs` — seam: image, connection-string builder, Respawn `DbAdapter`, master/admin connection string.
- `src/SolTechnology.Core.SQL.Testing/Engines/MsSqlEngine.cs` — port of current `SQLFixture` container/login-wait logic. **Keep the generic `ContainerBuilder` + host-side login-probe approach; do NOT take a dependency on `Testcontainers.MsSql`.** Preserve the documented azure-sql-edge/`sqlcmd`-missing rationale in remarks.
- `src/SolTechnology.Core.SQL.Testing/Engines/PostgresEngine.cs` — port of MTS `SqlFixture` + `DatabaseReadinessLogWaitStrategy`.
- `src/SolTechnology.Core.SQL.Testing/Provisioning/ISchemaProvisioner.cs` — strategy seam.
- `src/SolTechnology.Core.SQL.Testing/Provisioning/DacpacProvisioner.cs` — current dacpac deploy (`WithSQLProject`).
- `src/SolTechnology.Core.SQL.Testing/Provisioning/EfMigrationProvisioner.cs` — runs EF migrations against the container (port of KYC `PostgresMigrationRunner`).
- `src/SolTechnology.Core.SQL.Testing/Provisioning/ScriptProvisioner.cs` — raw `.sql` scripts.
- `src/SolTechnology.Core.SQL.Testing/SqlReset.cs` — Respawn checkpoint wrapper, engine-aware adapter. **Scope the reset to the application catalog only** so a shared MSSQL instance's Service Bus emulator catalog is never truncated.
- `src/SolTechnology.Core.Sql/SolTechnology.Core.SQL.csproj` — remove `Testing/`, drop the test-only `Testcontainers` dependency. **DacFx STAYS** (runtime dependency, used by `SQLProjectDeployer`). Bump version to the next MINOR; note the type-removal is breaking-in-principle but test-only and pre-1.0.
- Delete `src/SolTechnology.Core.Sql/Testing/SQLFixture.cs` and the obsolete `SQLConfiguration`-coupled test wait strategy if local.

## Details
- Fluent API stays source-compatible: `new SQLFixture("Db").WithSQLProject(path)` keeps working (MSSQL + dacpac defaults). Add `.UsePostgres()`, `.WithEfMigrations<TContext>()`, `.WithScripts(paths)`.
- Dapper vs EF is **not** modelled — the fixture only hands back a connection string. EF appears solely as one `ISchemaProvisioner`.
- **Shared MSSQL for the Service Bus emulator (step 08):** `SQL.Testing` owns the MSSQL container and exposes it via `ISharedSqlContainer` (container handle + shared network). The emulator's backing database MUST live in an **isolated catalog**, and `SqlReset`'s Respawn checkpoint MUST be scoped to the **application catalog only**, never the emulator's tables.
- **Consume the shared lifetime model from step 02**: use `TestContainersContext` reuse policy + `ContainerReuse` (stable name, semaphore-guarded one-time init, cached connection string) + `ContainerLifecycleHelper.EnsureRunningAsync`. When `TESTCONTAINERS_REUSE=true`, the DB container is reused across runs and **schema provisioning + reset run once per reused container** (re-provision only on a fresh container); dispose is a no-op. Do not re-implement reuse here.
- Respawn reset must work for both engines (MSSQL `SqlServerDbAdapter`, Postgres `PostgresDbAdapter`); replace the KYC raw-`TRUNCATE` and TaleCode docs Respawn usage with this one helper. `SqlReset` is the between-test reset path even when the container is reused.
- Keep the host-side-only wait strategy rationale documented (see existing `SQLFixture` remarks).
- **No test project.** Per ADR-008 there is intentionally no `tests/SolTechnology.Core.SQL.Testing.Tests`; the MSSQL/Postgres paths are validated by **documented manual smoke checks** (a container starts and returns a usable connection string), not automated CI runs. Nothing is added to `tests/`, so PR/CI builds are unaffected.

## Acceptance criteria
- `dotnet build src/SolTechnology.Core.SQL.Testing` and `src/SolTechnology.Core.Sql` both succeed.
- A documented manual smoke confirms MSSQL+dacpac and Postgres+EF-migrations paths each spin a container and return a usable connection string.
- With `TESTCONTAINERS_REUSE=true`, a documented manual second run reuses the container and skips re-provisioning; with it off, the container is disposed.
- `SqlReset` empties the application catalog's non-schema tables on both engines and **leaves any shared emulator catalog untouched** (documented manual smoke).
- `ISharedSqlContainer` exposes the MSSQL container + network for `ServiceBus.Testing`.
- `SolTechnology.Core.Sql` runtime output no longer references Testcontainers, but **still references DacFx** (used by `SQLProjectDeployer`).

## Open questions
- Confirm Respawn version via `package-management` skill.

