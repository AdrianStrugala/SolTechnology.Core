---
spec: 2026-11-02-core-2-0-messagebus
step: 02
status: done
---

# Step 02: MessageBus — resolvable broker, registered health-check client, correlation service

## Summary
Any host that calls `AddSolMessageBus` currently fails at its first resolve. MS.DI cannot bind
the `internal` constructor of `MessageBusBroker` (`B1`, `Broker/MessageBusBroker.cs:28`). The
health check resolves a `ServiceBusClient` that nobody registers (`B2`,
`HealthChecks/ServiceBusHealthCheckExtensions.cs:33`). The publisher and receiver also need an
`ICorrelationIdService` the installer never adds (`infra#13`). Per design A5, the broker is
registered through a factory, so no public surface is added. The shared client keeps today's
`TransportType` and `RetryOptions` (`MessageBusBroker.cs:42-50`) and never reuses a
`ServiceBusClient` the consumer registered for its own purposes.

Former core step 12; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.MessageBus/ModuleInstaller.cs` — EDIT — factory registration, keyed `ServiceBusClient` singleton, `AddSolCorrelationIdService()`
- `src/SolTechnology.Core.MessageBus/Broker/MessageBusBroker.cs` — EDIT — takes the shared `ServiceBusClient` instead of creating its own (`:42-50` client options move to the factory); `DisposeAsync` (`:232`) stops disposing the client
- `src/SolTechnology.Core.MessageBus/Broker/MessageBusClientFactory.cs` — NEW — internal; builds the client from `IOptions<MessageBusConfiguration>` with `TransportType` and `RetryOptions`
- `src/SolTechnology.Core.MessageBus/HealthChecks/ServiceBusHealthCheckExtensions.cs` — EDIT — `:33` resolves the keyed client
- `tests/SolTechnology.Core.MessageBus.Tests/ModuleResolutionTests.cs` — EDIT (created in step 01) — remove the B1, B2, and infra#13 ignores

## Changes
- Client: `services.TryAddKeyedSingleton<ServiceBusClient>(MessageBusClientFactory.ServiceKey, (sp, _) => MessageBusClientFactory.Create(sp.GetRequiredService<IOptions<MessageBusConfiguration>>().Value))`.
  - `ServiceKey` is an `internal const string`, so a consumer's own unkeyed `ServiceBusClient` (for example from `AddAzureClients`) is never picked up or replaced.
  - Options are read when the client is resolved, not at registration time.
  - `Create` applies `ServiceBusClientOptions { TransportType = …, RetryOptions = … }` exactly as `MessageBusBroker.cs:42-50` does today.
  - DI owns and disposes the client; the broker no longer disposes it.
- Broker: `services.TryAddSingleton<IMessageBusBroker>(sp => new MessageBusBroker(sp.GetRequiredService<IOptions<MessageBusConfiguration>>(), sp.GetRequiredService<MessageBusRegistry>(), sp.GetRequiredKeyedService<ServiceBusClient>(MessageBusClientFactory.ServiceKey)))`. The constructor stays `internal`.
- Tests (in `ModuleResolutionTests` or `ModuleInstallerTests`):
  - `AddSolMessageBus_ConsumerRegisteredServiceBusClient_IsNotReused`.
  - `AddSolMessageBus_Client_UsesConfiguredTransportAndRetry`.
  - `Broker_DisposeAsync_DoesNotDisposeSharedClient`.
- Call the existing idempotent `services.AddSolCorrelationIdService()` (`Logging/ModuleInstaller.cs:131`).
- `docs/Bus.md:111` is corrected in step 06.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green with no `[Ignore]` left in `ModuleResolutionTests`.
- [x] `git diff` shows no new `public` or `protected` member in `src/SolTechnology.Core.MessageBus`.
- [x] Premortem mitigations for `ModuleInstaller.cs` (core step 00) are applied. No `RM-n` targets this step; core premortem scenario 25 (`ServiceBusClient` lifetime) is defended by `AddSolMessageBus_ConsumerRegisteredServiceBusClient_IsNotReused` and `Broker_DisposeAsync_DoesNotDisposeSharedClient`.

## Open questions
- none

## Deviations

### Client options exposed to the test through an internal method

**Original plan:** `MessageBusClientFactory.Create` applies the transport and retry options, and `AddSolMessageBus_Client_UsesConfiguredTransportAndRetry` checks them on the client.

**Actual implementation:** `ServiceBusClient` exposes `TransportType` but not its retry options, so the factory builds them in an internal `CreateOptions` that `Create` uses. The test checks `TransportType` on the resolved client and the retry options through `CreateOptions`, with no reflection into the Azure SDK. The empty-`ConnectionString` `ArgumentException` that the broker constructor threw moved unchanged into `Create`, because the client is now built before the broker.
