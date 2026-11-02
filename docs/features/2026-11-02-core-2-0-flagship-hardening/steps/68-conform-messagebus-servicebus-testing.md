---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 68
status: to-do
---

# Step 68: Guide conformance — MessageBus and ServiceBus.Testing

## Summary
MessageBus has the same Guide violations as the other modules (`infra#24`):

- Size: `MessageBusBroker.cs` (237), `ServiceBusFixture.cs` (230), `MessageBusReceiver.cs`
  (190, with `HandleMessageAsync` at about 90 lines), and `MessageBus/ModuleInstaller.cs` (152;
  steps 12 and 34 add to it).
- Hand-written constructors and `static class` + `this`.
- Two `catch { }` blocks swallow exceptions (`MessageBusBroker.cs:221,228`, §13).
- Unbraced `if`s and essay comments.

The SQL, Blob, Hangfire, and testing-kit half follows in step 69 (split from the former single
infrastructure conformance step during plan review, RQ11).

## Affected components
- `src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs` — EDIT — `MessageDeserializer.cs`, `MessageSettler.cs`, `HandlerInvoker.cs` (NEW, internal)
- `src/SolTechnology.Core.MessageBus/Broker/MessageBusBroker.cs` — EDIT — split below 150; `:221,228` log at Warning; `:28-56` primary constructor
- `src/SolTechnology.Core.MessageBus/ModuleInstaller.cs` — EDIT — `:104,140-143` braces and comments; `extension` block; below 150 lines (move the endpoint-registration helpers into an internal file, NEW)
- `src/SolTechnology.Core.ServiceBus.Testing/ServiceBusFixture.cs` — EDIT — split below 150

## Changes
- No public or protected signature change; no log-template change except the two new Warning entries for the formerly swallowed exceptions.
- Premortem RM-16 (step 00, 2026-11-02, scenario 16): moving the endpoint-registration helpers out of `ModuleInstaller.cs` keeps the configuration lookup that matches `QueueConfiguration.MessageType` against `typeof(TMessage).Name` (`MessageBus/ModuleInstaller.cs:106`). Changing it to `FullName` would leave every configured queue unmatched. The receiver split keeps the settlement tests from step 34 (`Publish_ThenReceive_HandlerCalledAndMessageCompleted`, `Receive_HandlerThrows_AbandonedThenDeadLetteredOnLastAttempt`, `Receive_HandlerThrowsSync_IsNotWrapped`) unchanged.

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.MessageBus src/SolTechnology.Core.ServiceBus.Testing -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `grep -rnE "catch\s*\{\s*\}" src/SolTechnology.Core.MessageBus` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green (Docker).
- [ ] Public API diff is empty.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [ ] RM-16: `grep -rn "typeof(TMessage).Name" src/SolTechnology.Core.MessageBus` matches; `git diff` of `tests/SolTechnology.Core.MessageBus.Tests/MessageBusReceiverTests.cs` for this step is empty.

## Open questions
- none

## Deviations
