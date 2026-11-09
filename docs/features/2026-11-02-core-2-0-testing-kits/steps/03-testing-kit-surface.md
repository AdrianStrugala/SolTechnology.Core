---
spec: 2026-11-02-core-2-0-testing-kits
step: 03
status: done
---

# Step 03: Testing kits — 2.0 surface (optional `Async` renames, WireMock fields)

## Summary
The test-kit fixtures expose `Async`-suffixed methods with no sync overload (Guide §10).
`ServiceBusFixture.InitializeAsync()` takes no token, unlike its siblings. `WireMockStartup`
exposes a public mutable `WireMockServer` field, and `FakeApiBase.BaseUrl` is a required abstract
member that nothing reads (`http-api` semver notes, `infra` semver notes). All of these are
optional (`C-23`, `C-24`).

Former core step 53; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Blob.Testing/AzuriteFixture.cs` — EDIT — `Initialize`, `Clear`, `CreateBlobContainer`
- `src/SolTechnology.Core.Redis.Testing/RedisFixture.cs` — EDIT — `Initialize`, `Flush`
- `src/SolTechnology.Core.SQL.Testing/SQLFixture.cs` — EDIT — `Initialize`, `Reset`
- `src/SolTechnology.Core.ServiceBus.Testing/ServiceBusFixture.cs` — EDIT — `Initialize(CancellationToken ct = default)`
- `src/SolTechnology.Core.Testing/Containers/ContainerLifecycleHelper.cs` — EDIT — `EnsureRunning`, `WaitForAmqpReady`, `WaitForHealthy`
- `src/SolTechnology.Core.HTTP.Testing/WireMockStartup.cs` — EDIT — `WireMockServer` → get-only property
- `src/SolTechnology.Core.HTTP.Testing/Faker/FakeAPIBase.cs` — EDIT — DELETE `BaseUrl`
- `tests/**`, `sample-tale-code-apps/DreamTravel/tests/**` — EDIT — call sites

## Changes
- `DisposeAsync` stays (framework `IAsyncDisposable`).

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green; `pwsh ./.github/runTests.ps1` is green (Docker).

## Open questions
- none (optional `C-23`, `C-24` confirmed by the user on 2026-11-02)

## Deviations

### Scope kept to the `C-23` and `C-24` lists

**Original plan:** drop `Async` from the listed fixture and helper methods; `ServiceBusFixture.Initialize` gains a token;
`WireMockServer` becomes a get-only property; `FakeApiBase.BaseUrl` is deleted.

**Actual implementation:** as listed, and nothing more. `RedisFixture.Flush` keeps its parameterless signature (the row
only renames it). The internal `IDatabaseEngine.StartAsync/EnsureDatabaseAsync`, `ISchemaProvisioner.ProvisionAsync` and
`SQLReset.ResetAsync` keep their names: they are not public, and renaming them would only widen the diff.
`WireMockStartup.WireMockServer` is `{ get; private set; }`, because `Run` assigns it. The exception messages that named
the old methods ("Call InitializeAsync first.") now name the new ones. `AzuriteFixture` keeps the default image
`azurite:3.37.0` (blob code review pin). DreamTravel's `GoogleFakeApi` keeps its `"google"` prefix as a private
constant. `ServiceBusFixture.Initialize` passes the token to its start lock, the Docker.DotNet reuse calls and
`StartAsync`; `ContainerLifecycleHelper.WaitForAmqpReady` takes none (the row adds none).

Verification: `dotnet build SolTechnology.Core.slnx -c Release` is green, and the seven kits build with no warnings.
Instead of the whole `runTests.ps1`, every project that calls a renamed member ran (`-c Release`): HTTP.Tests 158/158,
Cache.Tests 56/56, SQL.Tests 19/19, Hangfire.Tests 73/73, MessageBus.Tests 40/40 and the DreamTravel component tests
31/31; the full suite runs after step 06.
