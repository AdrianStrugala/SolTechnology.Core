---
spec: 2026-11-02-core-2-0-sql
step: 04
status: to-do
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
- [ ] `dotnet build SolTechnology.Core.slnx` is green; `dotnet test tests/SolTechnology.Core.SQL.Tests` is green.
- [ ] `grep -n "Console\." src/SolTechnology.Core.SQL -r` prints nothing.

## Open questions
- none (`C-16`, `C-17` confirmed by the user on 2026-11-02)

## Deviations
