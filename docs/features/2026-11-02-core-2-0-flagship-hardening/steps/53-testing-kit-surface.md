---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 53
status: to-do
---

# Step 53: Testing kits — 2.0 surface (optional `Async` renames, WireMock fields)

## Summary
The test-kit fixtures expose `Async`-suffixed methods with no sync overload (Guide §10).
`ServiceBusFixture.InitializeAsync()` takes no token, unlike its siblings. `WireMockStartup`
exposes a public mutable `WireMockServer` field, and `FakeApiBase.BaseUrl` is a required abstract
member that nothing reads (`http-api` semver notes, `infra` semver notes). All of these are
optional (`C-23`, `C-24`).

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
- [ ] `dotnet build SolTechnology.Core.slnx` is green; `pwsh ./.github/runTests.ps1` is green (Docker).

## Open questions
- none (optional `C-23`, `C-24` confirmed by the user on 2026-11-02)

## Deviations
