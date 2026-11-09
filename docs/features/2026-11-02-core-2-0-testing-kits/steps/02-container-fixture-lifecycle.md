---
spec: 2026-11-02-core-2-0-testing-kits
step: 02
status: done
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
- [x] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green (Docker).
- [x] `grep -rn "Ryuk" src/SolTechnology.Core.Testing` shows no programmatic override.
- [ ] `pwsh ./.github/runTests.ps1` is green on CI (ubuntu, Ryuk enabled).

## Open questions
- none

## Deviations

### The fixture owns the MSSQL sidecar and network (messagebus follow-up absorbed)

**Original plan:** instance state in `ServiceBusFixture`; clear the shared entries on dispose when reuse is off.

**Actual implementation:** all initialization state (connection string, initialized flag) is per instance, so there
are no shared entries left to clear; only a per-name `SemaphoreSlim` stays static, because reuse mode pins one Docker
container per name. The step also absorbs the messagebus follow-up: `Testcontainers.ServiceBus` 4.16 `Build()` creates
an MSSQL sidecar and a network that `ServiceBusContainer.DisposeAsync` never removes (it only disposes the emulator),
so with Ryuk off they piled up. The fixture now builds both itself (`MsSqlBuilder` with the image
`2022-CU14-ubuntu-22.04` that Testcontainers uses, through `WithMsSqlContainer`) and removes the emulator, the sidecar
and the network on dispose, and also when the start fails. With Ryuk off, Testcontainers only stops a disposed
container and keeps a disposed network, so the sidecar gets `WithAutoRemove` like the emulator and the network is
deleted explicitly. In reuse mode they get the names `{name}-mssql` and `{name}-network`, and the Docker.DotNet
reuse helpers (find, start if stopped, remove a stale emulator with its sidecar and network) move into the internal
`ReusedEmulator`; `ServiceBusFixture` drops from 230 to 179 lines (step 06 takes it below 150). `MsSqlBuilder` comes
from `Testcontainers.MsSql`, a dependency of `Testcontainers.ServiceBus`, so no package reference is added.

### Ryuk was off for every fixture, not only for aborted runs

**Original plan:** delete the Ryuk override in `TestContainersContext`.

**Actual implementation:** deleted. Testcontainers' `WithCleanUp(true)` sets no session id while Ryuk is disabled, and
`DisposeAsync` then only stops a container. Because the override ran in `TestContainersContext`'s static constructor,
which every fixture triggers through `ReuseContainers`, every disposed SQL, Redis and Azurite container was left
stopped as well (dozens of `Exited` containers on the dev box). With Ryuk on by default they are removed. This is a
consumer-visible change, recorded as `P-28`; the `ServiceBusFixture` change is `P-29` (both new rows in the core
brief, with a release-notes entry). `docs/Testing.md` documents `TESTCONTAINERS_RYUK_DISABLED=true` and the
stopped-container caveat; `docs/ServiceBus.Testing.md` describes the sidecar ownership, because its "the emulator owns
its sidecar" text became wrong in this step.

### The lifecycle test also checks the sidecar

`ServiceBusFixtureLifecycleTests` starts two fixtures with the same name, disposes the first, and checks that the
second has its own connection string and accepts a message, and that the first's emulator, sidecar and network are
gone. It is skipped with `TESTCONTAINERS_REUSE=true`. Against the old fixture it fails on the shared connection string.
It passes with Ryuk on and with `TESTCONTAINERS_RYUK_DISABLED=true`.

### CI run pending

The third criterion needs a CI run on ubuntu; nothing is pushed in this delivery, so it stays open for the push. Locally
every consumer ran with Ryuk on (macOS arm64, Docker Desktop): MessageBus.Tests 40/40, SQL.Tests 19/19, Cache.Tests
56/56, CQRS.Tests 56/56, Hangfire.Tests 73/73 and the DreamTravel component tests 31/31 (`-c Release`); no container or
network was left behind. Release builds of Testing, ServiceBus.Testing and MessageBus.Tests have no warnings.
