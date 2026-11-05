---
spec: 2026-11-02-core-2-0-sql
step: 04
status: done
---

# Step 04: SQL — internal implementations; safe-by-default DACPAC deployment

## Summary
`SQLConnectionFactory` and `UnitOfWork` are public implementation types behind
`ISQLConnectionFactory` and `IUnitOfWork` (`C-16`). `SQLProjectDeployer.DeployDacpacAsync` ships
in a production package but has unsafe behaviour (`infra#12`,
`SQLProject/SQLProjectDeployer.cs:8-39`, `C-17`, L-04):

- It deploys with `BlockOnPossibleDataLoss = false`, so a schema change can silently drop data.
- It logs through `Console.WriteLine`.
- Its `CancellationToken` never reaches `DacServices.Deploy`.
- Its name carries the `Async` suffix.

Former core step 49; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.SQL/Connections/SQLConnectionFactory.cs` — EDIT — `internal sealed`
- `src/SolTechnology.Core.SQL/Transactions/UnitOfWork.cs` — EDIT — `internal sealed`
- `src/SolTechnology.Core.SQL/SQLProject/SQLProjectDeployer.cs` — EDIT — `DeployDacpac(string dacpacPath, string connectionString, string databaseName, ILogger logger, DacDeployOptions? deployOptions = null, CancellationToken ct = default)`; default `BlockOnPossibleDataLoss = true`; `ct` passed to `Deploy`
- `src/SolTechnology.Core.SQL.Testing/Provisioning/DacpacProvisioner.cs` — unchanged — it calls `DacServices` directly (`:42`), not `SQLProjectDeployer`
- `sample-tale-code-apps/DreamTravel/src/DreamTravel.Aspire/Program.cs` — EDIT — `:107` the only call site: `SQLProjectDeployer.DeployDacpac(dacpacPath, connectionString, "DreamTravelDatabase", logger, ct: ct)`, with `logger = @event.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DreamTravel.DacpacDeployment")` inside the `OnResourceReady` callback
- `tests/SolTechnology.Core.SQL.Tests/SQLProjectDeployerTests.cs` — NEW — `DeployDacpac_DataLossChange_IsBlockedByDefault` (`SQLFixture` + a test dacpac, or a unit test asserting the default options when a dacpac fixture is impractical; record which)

## Changes
- Log templates use `[{DacpacPath}]`, `[{DatabaseName}]` (Guide §11).

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green; `dotnet test tests/SolTechnology.Core.SQL.Tests` is green.
- [x] `grep -n "Console\." src/SolTechnology.Core.SQL -r` prints nothing.

## Open questions
- none (`C-16`, `C-17` confirmed by the user on 2026-11-02)

## Deviations

### Safe defaults also stop recreating the database

**Original plan:** default `BlockOnPossibleDataLoss = true`; the other 1.x options were not mentioned.

**Actual implementation:** without `deployOptions` the deployer uses `new DacDeployOptions { BlockOnPossibleDataLoss = true }`,
so `CreateNewDatabase` and `DropObjectsNotInSource` keep their DacFx default `false`. The 1.x options set
`CreateNewDatabase = true`, which drops and recreates an existing database on every deploy and would lose its data
whatever `BlockOnPossibleDataLoss` says. An existing database is now upgraded in place; a missing one is still created.
DreamTravel's persistent Aspire SQL container keeps its data across restarts as a result (its dacpac has no
post-deployment scripts). The `Message` event logs DacFx errors as `Error`, warnings as `Warning`, and other messages as
`Information`; `ProgressChanged` logs at `Debug`.

### Real dacpac tests, run in parallel

**Original plan:** `DeployDacpac_DataLossChange_IsBlockedByDefault`, with `SQLFixture` and a test dacpac or a unit test
asserting the default options; record which.

**Actual implementation:** both tests deploy a real dacpac to the shared `SQLFixture` server. Each creates its own database
with a `Things` table holding a row, and builds a dacpac without the `Name` column in memory through the DacFx
`TSqlModel` API, so no `.sqlproj` build is needed. `DeployDacpac_DataLossChange_IsBlockedByDefault` expects a
`DacServicesException` and the row unchanged; it failed with the 1.x options ("no exception was thrown").
`DeployDacpac_DataLossAllowedByOptions_UpgradesInPlaceAndLogsBracketedTemplates` pins the override, the in-place
upgrade, and the L-04 templates through a capturing `ILogger`. On the arm64 dev box with the emulated SQL Server image each
deploy spends about 2.5 minutes in DacFx target-model extraction (server-side `RESOURCE_SEMAPHORE` waits), so the fixture
runs its two tests in parallel.

