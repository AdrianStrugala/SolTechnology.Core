---
adr: 008-testing-framework-companions
step: 08 of 11
status: reviewed
---

<!-- Reviewed: renumbered from to-do/07-servicebus-testing.md. Shared MSSQL now flows through
     SQL.Testing's ISharedSqlContainer (isolated emulator catalog); step-01 refs → step 02. -->

# Step 08: `SolTechnology.Core.ServiceBus.Testing` (Azure Service Bus emulator)

## Summary
Package the Azure Service Bus emulator fixture — the most lifetime-sensitive of the lot — as a
companion of the messaging stack. This is where the KYC reuse/restart/AMQP-readiness work pays off
most, because the emulator is slow to boot and depends on a sibling MSSQL container. Single PR — one
fixture plus its emulator builder; the AMQP readiness probe itself lives in `Core.Testing` (step 02)
and is consumed here.

## Affected components
- `src/SolTechnology.Core.ServiceBus.Testing/SolTechnology.Core.ServiceBus.Testing.csproj` — new package (`Testcontainers.ServiceBus`, `Docker.DotNet`), version `0.1.0`. Depends on `SolTechnology.Core.Testing` **and `SolTechnology.Core.SQL.Testing`** (for the shared MSSQL container via `ISharedSqlContainer`). **No `Testcontainers.MsSql`** — the sibling MSSQL is provided by `SQL.Testing`'s generic-`ContainerBuilder` engine, not spawned here.
- `src/SolTechnology.Core.ServiceBus.Testing/ServiceBusFixture.cs` — port of KYC `Infrastructure/Containers/ServiceBusFixture.cs`. Generalise: remove `aiia-kyc-*` hard-coded names into a configurable instance name; keep multi-named-instance support.
- `src/SolTechnology.Core.ServiceBus.Testing/ServiceBusInstanceBuilder.cs` — port of KYC `ServiceBusInstanceBuilder` (emulator builder wired to the **shared** MSSQL container from `SQL.Testing` on the shared network).
- `src/SolTechnology.Core.ServiceBus.Testing/servicebus-emulator-config.json` — emulator topology config (port of KYC's, made consumer-overridable).
- `docs/Bus.md` — note the companion (full pass in step 11).
- `SolTechnology.Core.slnx` — add project.

## Details
- **Lifetime is the headline.** Carry over verbatim from KYC:
  - **AMQP SASL-echo readiness probe** (`ContainerLifecycleHelper.WaitForAmqpReadyAsync` from step 02) — TCP-accept alone causes `NullReferenceException` in `AmqpTransportInitiator`; do not regress this.
  - **Reuse via stable container name + manual Docker.DotNet management, local to this fixture** (not Testcontainers' reuse hash, which is unstable because of the `MsSqlContainer` reference in `DependsOn`): on first run create via Testcontainers with a fixed name; on subsequent runs detect by name, start-if-stopped, re-read mapped port, rebuild connection string. This bespoke detection lives **inside `ServiceBus.Testing`** — it is the reason there is no shared `ContainerReuse` helper (the other fixtures rely on Testcontainers-native `.WithReuse`, which works for them).
  - **Semaphore-guarded, per-instance one-time init** + `ConcurrentDictionary` caches for connection strings and initialized flags — needed here because a single fixture instance may be asked to provide multiple named emulator instances; these caches are private to this fixture.
  - **Restart-if-stopped** via `ContainerLifecycleHelper.EnsureRunningAsync` (handles Docker Desktop stops).
  - **Dispose is a no-op when `TESTCONTAINERS_REUSE` is on.**
- **Shared MSSQL contract (resolved in step 03).** The fixture consumes the MSSQL container exposed by `SQL.Testing` via `ISharedSqlContainer` rather than spawning a second instance. The emulator's backing database is an **isolated catalog**; `SQL.Testing`'s `SqlReset` is scoped to the application catalog only, so a between-test reset never truncates the emulator's tables.
- Keep app-specific topology (queue/topic names) out of the package — config is consumer-supplied.
- **No test project.** Per ADR-008 there is intentionally no `tests/SolTechnology.Core.ServiceBus.Testing.Tests`; validation is build-based plus a documented manual smoke (message round-trip gated on the AMQP probe). Nothing is added to `tests/`, so PR/CI builds are unaffected.

## Acceptance criteria
- `dotnet build src/SolTechnology.Core.ServiceBus.Testing` succeeds.
- A documented manual smoke round-trips a message (send + receive) through the emulator, gated on the AMQP readiness probe.
- With `TESTCONTAINERS_REUSE=true`, a documented manual second run reuses the emulator by name and skips re-creation; with it off, the container is disposed.
- The fixture consumes the shared MSSQL from `SQL.Testing` (no second MSSQL container, no `Testcontainers.MsSql` dependency).
- No `aiia-kyc` / app-specific identifiers remain in the package.

## Open questions
- none — the shared-MSSQL contract is now an `ISharedSqlContainer` interface exposed by `SQL.Testing` (resolved in step 03).

