---
spec: 2026-11-02-core-2-0-messagebus
step: 04
status: done
---

# Step 04: MessageBus — internal implementation types

## Summary
`MessageBusBroker`, `MessagePublisher`, and `MessageBusReceiver` are public implementation types
that nobody needs to construct. `IMessageBusBroker` leaks the transport types `ServiceBusSender`
and `ServiceBusProcessor` (`infra` semver notes, Guide §5). 2.0 makes all four internal, so only
`IMessagePublisher`, `IMessageHandler<T>`, `IMessage`, the configuration types, and the installers
stay public (`C-14`).

Former core step 48; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.MessageBus/Broker/MessageBusBroker.cs` — EDIT — `internal sealed`
- `src/SolTechnology.Core.MessageBus/Broker/IMessageBusBroker.cs` — EDIT — `internal`
- `src/SolTechnology.Core.MessageBus/Publish/MessagePublisher.cs` — EDIT — `internal sealed`
- `src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs` — EDIT — `internal sealed`
- `src/SolTechnology.Core.MessageBus/SolTechnology.Core.MessageBus.csproj` — EDIT — `InternalsVisibleTo` `SolTechnology.Core.MessageBus.Tests` (only if tests need it)
- `tests/SolTechnology.Core.MessageBus.Tests/**` — EDIT

## Changes
- `ModuleResolutionTests` (step 01) still resolves `IMessagePublisher` and the hosted service.

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green, including DreamTravel.
- [x] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green.

## Open questions
- none (`C-14` confirmed by the user on 2026-11-02)

## Deviations

None. `InternalsVisibleTo` for `SolTechnology.Core.MessageBus.Tests` already existed, so the `.csproj`
needed no edit; the tests compile unchanged. Full suite: 855 passed, 11 skipped (DreamTravel live-API tests), 0 failed.
