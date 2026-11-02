---
spec: 2026-11-02-core-2-0-testing-kits
step: 02
status: to-do
---

# Step 02: Container fixtures — per-instance state; honour Ryuk settings

## Summary
Two container-fixture defects:

- `ServiceBusFixture` keeps initialization state in static dictionaries while each instance owns
  its container. A second instance with the same name gets the first instance's connection
  string, and after the first disposes, later fixtures point at a dead container (`infra#15`,
  `ServiceBusFixture.cs:31-33,66-79,117-123`).
- `TestContainersContext` disables Ryuk unconditionally through a static constructor, so aborted
  CI runs leak containers (`infra#16`, `Testing/Containers/TestContainersContext.cs:33-38`).

Former core step 37; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.ServiceBus.Testing/ServiceBusFixture.cs` — EDIT — instance state; clear shared entries on dispose when reuse is off
- `src/SolTechnology.Core.Testing/Containers/TestContainersContext.cs` — EDIT — delete the Ryuk override
- `docs/Testing.md` — EDIT — document `TESTCONTAINERS_RYUK_DISABLED=true` for Enhanced Container Isolation users
- `tests/SolTechnology.Core.MessageBus.Tests/ServiceBusFixtureLifecycleTests.cs` — NEW — two fixtures with the same name; dispose the first, the second still works

## Changes
- `ServiceBusFixture.InitializeAsync` keeps its signature (the `CancellationToken` overload is `C-23`, step 03).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green (Docker).
- [ ] `grep -rn "Ryuk" src/SolTechnology.Core.Testing` shows no programmatic override.
- [ ] `pwsh ./.github/runTests.ps1` is green on CI (ubuntu, Ryuk enabled).

## Open questions
- none

## Deviations
