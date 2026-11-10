---
spec: 2026-11-02-logging-chronicle
step: 13
status: done
---

# Step 13: MessageBus message roots

## Summary
Every received message becomes its own Chronicle root through `BeginRoot` (B11), started right
after the correlation ID is restored and inside the receiver's log scope, so handler steps, SQL
commands, and HTTP calls made while handling the message nest under it. The root status follows
the settlement that the 2.0 receiver chose: completed → `Ok`; dead-lettered or abandoned →
`Failed`; processor shutdown → `Cancelled`. The settler reports that outcome to the receiver,
which today only learns "handled or not". One EventId 2154 entry is emitted per message (LC-03).
The message body is never recorded.

## Affected components
- `src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs` — EDIT — primary constructor gains `IChronicle chronicle` (the type is `internal sealed` since messagebus step "MessageBus — internal implementation types", `C-14`, so no `CLAUDE.md §2` gate); the private `HandleMessageAsync(ProcessMessageEventArgs, Type)` becomes `internal` for tests and wraps `TryHandle` and `Complete` in `BeginRoot($"Message {messageType.Name}")`, after `RestoreCorrelation` and inside the existing log scope; `TryHandle` returns the settlement outcome instead of `bool`
- `src/SolTechnology.Core.MessageBus/Receive/MessageSettler.cs` — EDIT — `Complete`, `DeadLetterUnreadable`, and `SettleFailure` return a `MessageSettlement` (decision R4) instead of `void`/`Task`; `TrySettle` already knows whether the broker call succeeded
- `src/SolTechnology.Core.MessageBus/Receive/MessageSettlement.cs` — NEW — `internal readonly record struct MessageSettlement(MessageSettlementAction Action, string? DeadLetterReason, bool Settled)` and `internal enum MessageSettlementAction { Completed, Abandoned, DeadLettered }`
- `src/SolTechnology.Core.MessageBus/ModuleInstaller.cs` — EDIT — `AddSolMessageBus` calls `services.AddSolChronicle()` next to `AddSolCorrelationIdService()` (CP-03); stays ≤ 150 lines (132 at `c30d8998`)
- `tests/SolTechnology.Core.MessageBus.Tests/SolTechnology.Core.MessageBus.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (version from `Directory.Packages.props`, added by step 04)
- `tests/SolTechnology.Core.MessageBus.Tests/MessageBusReceiverChronicleTests.cs` — NEW — unit level, no emulator: messages from `ServiceBusModelFactory.ServiceBusReceivedMessage`, `ProcessMessageEventArgs` over an NSubstitute `ServiceBusReceiver`, `HandleMessageAsync` called directly
- `tests/SolTechnology.Core.MessageBus.Tests/MessageSettlerTests.cs` — EDIT — assert the returned `MessageSettlement`
- `tests/SolTechnology.Core.MessageBus.Tests/ModuleInstallerTests.cs` — EDIT (the testing-kits code review split endpoint tests into `ModuleInstallerEndpointTests.cs`; registration tests stay here)

## Changes
- Root attributes: `MessageId`, `DeliveryCount`, `Entity` (`args.EntityPath`), `Settlement` (`Completed`, `Abandoned`, `DeadLettered`), and `DeadLetterReason` when dead-lettered. No body, no application properties.
- Outcome mapping (decision R4; settlement rules of `docs/architecture/infrastructure-modules.md` and P-17):
  - Handler succeeded and the message was completed → `Ok`.
  - Handler succeeded but `CompleteMessageAsync` failed (the settler logs a Warning and the lock
    expires, so the message is delivered again) → `Ok` with `Settlement=CompleteFailed`.
  - Unreadable message dead-lettered (`EmptyBody`, `DeserializationFailed`, `NullPayload`) →
    `Fail("DeadLettered", reason)`. Only the reason code is recorded, never the dead-letter
    description, which can carry the `JsonException` text.
  - Handler exception while `DeliveryCount < MaxDeliveryCount` → `Fail(exception)` with `Settlement=Abandoned`.
  - Handler exception on the last delivery (`DeliveryCount >= MaxDeliveryCount`, P-17) →
    `Fail(exception)` with `Settlement=DeadLettered` and `DeadLetterReason=HandlerFailedMaxDelivery`.
  - `OperationCanceledException` while `args.CancellationToken` is cancelled (processor shutdown;
    the receiver rethrows without settling) → `Cancel()`.
- The 2154 entry is written inside the receiver's log scope, so it carries `CorrelationId`,
  `MessageId`, and `MessageType`. The receiver's async method restores the ambient node and the
  correlation when it returns, so it needs no `ICorrelationIdService.Clear()` (P-27 is for
  Hangfire's synchronous filters).
- Tests (`FakeLogCollector`; tree assertions on `FakeLogRecord.Message`):
  - `HandleMessage_Completed_EmitsOneRootReportOk`.
  - `HandleMessage_CompleteFails_RootOkWithCompleteFailed`.
  - `HandleMessage_HandlerThrows_RootFailedWithExceptionAndAbandoned`.
  - `HandleMessage_HandlerThrowsOnLastDelivery_RootFailedDeadLetteredHandlerFailedMaxDelivery`.
  - `HandleMessage_EmptyBody_RootFailedDeadLettered`.
  - `HandleMessage_InvalidJson_RootFailedDeadLetteredWithoutDescription`.
  - `HandleMessage_ProcessorStopping_RootCancelled`.
  - `HandleMessage_HandlerSteps_NestUnderMessageRoot`.
  - `HandleMessage_TwoConcurrentMessages_TwoIndependentRoots`.
  - `HandleMessage_Report_CarriesMessageIdAndDeliveryCountButNoBody`.
  - `AddSolMessageBus_RegistersChronicle`.
- The emulator-backed settlement tests (`MessageBusReceiverSettlementTests`, `ReceiverTestHost`) stay unchanged and must stay green.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.MessageBus.Tests --filter "FullyQualifiedName~Chronicle|FullyQualifiedName~MessageSettler|FullyQualifiedName~ModuleInstaller"` is green without Docker.
- [ ] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green with Docker available.
- [ ] `grep -n "public sealed class MessageBusReceiver" src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs` prints nothing (internal since `C-14`; satisfied at `c30d8998`).
- [ ] `wc -l < src/SolTechnology.Core.MessageBus/ModuleInstaller.cs` prints ≤ 150.
- [ ] `dotnet build SolTechnology.Core.slnx` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.

## Open questions
- none

## Deviations

### Root helper and settlement flow

**Original plan:** the receiver wraps `TryHandle` and `Complete` in `BeginRoot` and sets the root
attributes itself; `MessageSettlement` is a plain record struct.

**Actual implementation:** with the root code the receiver reached 153 lines, so the internal
static `MessageChronicle` opens the root (`Begin`: `MessageId`, `DeliveryCount`, `Entity`) and
records the settlement (`Record`: `Settlement`, `DeadLetterReason`); the receiver is 149 lines.
`TryHandle` takes the root and returns `MessageSettlement?`: `null` when the handler succeeded
(the receiver then completes the message), otherwise the settlement of the unreadable or failed
message. It fails the root itself (`Fail(exception)`, or `Fail("DeadLettered", reason)` for an
unreadable body), because only it sees the exception. A dead-letter of an unreadable message still
lets a broker failure propagate, as before (`Settled` is therefore always `true` there).

### Test layout

- `MessageBusReceiverChronicleTests` over `MessageBusReceiverChronicleTestBase` (real recorder,
  `FakeLogCollector`, a stub broker because `IMessageBusBroker` is internal and not proxyable).
  `HandleMessage_HandlerThrows_...` is parameterized on the delivery count (abandoned or
  dead-lettered on the last delivery), and `HandleMessage_UnreadableBody_...` on empty body,
  invalid JSON, and `null` (also `NullPayload`). `HandleMessage_HandlerSteps_NestUnderMessageRoot`
  and `HandleMessage_Report_CarriesMessageIdAndDeliveryCountButNoBody` are folded into
  `HandleMessage_Completed_EmitsOneRootReportOk`, which also checks the receiver's log scope on the
  2154 entry.
- `MessageSettlerTests.Settle_EveryOutcome_ReturnsSettlement` asserts the returned settlement.
- Existing tests: `MessageBusReceiverCompletionTests` passes a substitute `IChronicle`;
  `ModuleResolutionTests` now selects `MessageBusReceiver` among the hosted services, because
  `AddSolChronicle` adds the SqlClient subscription as a second `IHostedService`.
- The emulator suite (`MessageBusReceiverSettlementTests`) is green unchanged: 57 tests with Docker.

