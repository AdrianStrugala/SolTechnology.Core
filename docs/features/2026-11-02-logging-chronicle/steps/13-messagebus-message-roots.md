---
spec: 2026-11-02-logging-chronicle
step: 13
status: to-do
---

# Step 13: MessageBus message roots

## Summary
Every received message becomes its own Chronicle root through `BeginRoot` (B11), started right
after the correlation ID is restored, so handler steps, SQL commands, and HTTP calls made while
handling the message nest under it. The root status follows the receiver's settlement outcome as
defined by the 2.0 MessageBus work: completed → `Ok`, dead-lettered or abandoned → `Failed`,
processor shutdown → `Cancelled`. One EventId 2150 entry is emitted per message (LC-03). The
message body is never recorded.

## Affected components
- `src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs` — EDIT — constructor gains `IChronicle chronicle` (internal type after `2026-11-02-core-2-0-messagebus` step "MessageBus — internal implementation types"); `HandleMessageAsync` becomes `internal` for tests and wraps handling in `BeginRoot($"Message {messageType.Name}")`. If `2026-11-02-core-2-0-messagebus` step "Guide conformance — MessageBus" moved per-message handling into another internal type, edit that type instead and list it under `Deviations`.
- `src/SolTechnology.Core.MessageBus/ModuleInstaller.cs` — EDIT — `AddSolMessageBus` calls `services.AddSolChronicle()` (CP-03)
- `tests/SolTechnology.Core.MessageBus.Tests/SolTechnology.Core.MessageBus.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (same version as step 04)
- `tests/SolTechnology.Core.MessageBus.Tests/MessageBusReceiverChronicleTests.cs` — NEW — messages from `ServiceBusModelFactory.ServiceBusReceivedMessage`, `ProcessMessageEventArgs` over an NSubstitute `ServiceBusReceiver`
- `tests/SolTechnology.Core.MessageBus.Tests/ModuleInstallerTests.cs` — EDIT

## Changes
- Root attributes: `MessageId`, `DeliveryCount`, `Entity` (`args.EntityPath`). No body, no application properties.
- Outcome mapping (settlement rules owned by `2026-11-02-core-2-0-messagebus` step "MessageBus — receiver settlement matches its contract; publish→receive round-trip"):
  - Completed → `Ok`.
  - Dead-lettered (empty body, invalid JSON, handler failure past `MaxDeliveryCount`) → `Fail("DeadLettered", reason)`.
  - Handler exception that leads to abandon → `Fail(exception)`.
  - `args.CancellationToken` cancelled → `Cancel()`.
- Logs inside the root keep today's scope (`CorrelationId`, `MessageId`, `MessageType`).
- Tests:
  - `HandleMessage_Completed_EmitsOneRootReportOk`.
  - `HandleMessage_HandlerThrows_RootFailedWithException`.
  - `HandleMessage_EmptyBody_RootFailedDeadLettered`.
  - `HandleMessage_InvalidJson_RootFailedDeadLettered`.
  - `HandleMessage_HandlerSteps_NestUnderMessageRoot`.
  - `HandleMessage_TwoConcurrentMessages_TwoIndependentRoots`.
  - `HandleMessage_Report_CarriesMessageIdAndDeliveryCountButNoBody`.
  - `AddSolMessageBus_RegistersChronicle`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green.
- [ ] `dotnet build SolTechnology.Core.slnx` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.

## Open questions
- none

## Deviations
