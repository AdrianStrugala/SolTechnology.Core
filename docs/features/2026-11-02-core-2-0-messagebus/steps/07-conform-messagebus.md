---
spec: 2026-11-02-core-2-0-messagebus
step: 07
status: done
---

# Step 07: Guide conformance — MessageBus

## Summary
MessageBus has the same Guide violations as the other modules (`infra#24`):

- Size: `MessageBusBroker.cs` (237), `MessageBusReceiver.cs` (190, with `HandleMessageAsync` at
  about 90 lines), and `MessageBus/ModuleInstaller.cs` (152; steps 02 and 03 add to it).
- Hand-written constructors and `static class` + `this`.
- Two `catch { }` blocks swallow exceptions (`MessageBusBroker.cs:221,228`, §13).
- Unbraced `if`s and essay comments.

Former core step 68 (MessageBus part); split per library on 2026-11-02. The `ServiceBus.Testing` part is testing-kits step 06; the MessageBus log templates are step 08.

## Affected components
- `src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs` — EDIT — `MessageDeserializer.cs`, `MessageSettler.cs`, `HandlerInvoker.cs` (NEW, internal)
- `src/SolTechnology.Core.MessageBus/Broker/MessageBusBroker.cs` — EDIT — split below 150; `:221,228` log at Warning; `:28-56` primary constructor
- `src/SolTechnology.Core.MessageBus/ModuleInstaller.cs` — EDIT — `:104,140-143` braces and comments; `extension` block; below 150 lines (move the endpoint-registration helpers into an internal file, NEW)

## Changes
- No public or protected signature change; no log-template change except the two new Warning entries for the formerly swallowed exceptions.
- Behaviour-preserving refactor; use the [`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.
- Premortem RM-16 (core step 00, 2026-11-02, scenario 16): moving the endpoint-registration helpers out of `ModuleInstaller.cs` keeps the configuration lookup that matches `QueueConfiguration.MessageType` against `typeof(TMessage).Name` (`MessageBus/ModuleInstaller.cs:106`). Changing it to `FullName` would leave every configured queue unmatched. The receiver split keeps the settlement tests from step 03 (`Publish_ThenReceive_HandlerCalledAndMessageCompleted`, `Receive_HandlerThrows_AbandonedThenDeadLetteredOnLastAttempt`, `Receive_HandlerThrowsSync_IsNotWrapped`) unchanged.

## Acceptance criteria
- [x] `find src/SolTechnology.Core.MessageBus -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] `grep -rnE "catch\s*\{\s*\}" src/SolTechnology.Core.MessageBus` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green (Docker).
- [x] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [x] RM-16: `grep -rn "typeof(TMessage).Name" src/SolTechnology.Core.MessageBus` matches; `git diff` of `tests/SolTechnology.Core.MessageBus.Tests/MessageBusReceiverTests.cs` for this step is empty.

## Open questions
- none

## Deviations

### Collaborators extracted beyond the plan

**Original plan:** split the broker below 150 lines and move the endpoint-registration helpers out of
`ModuleInstaller.cs` into one new internal file.

**Actual implementation:** the broker delegates to `ServiceBusEndpoints` (senders and processors, their disposal)
and `ServiceBusResourceProvisioner` (create-if-missing); the helpers live in `Configuration/EndpointRegistrar.cs`.
Both collaborators log under the broker's category, so the log categories do not change. The receiver now
takes `IOptions<MessageBusConfiguration>` through its constructor instead of resolving it on each failure; the
options are a singleton, so the delivery limit is the same value.

### Health-check extension and comments in other files

**Original plan:** `extension` block, braces, and comments in `ModuleInstaller.cs` only.

**Actual implementation:** `ServiceBusHealthCheckExtensions` also moves into an `extension` block, and the
multi-line comments in `ServiceBusHealthCheck.cs` and `MessagePublisher.cs` shrink to one line each, because they
are the same §9.7 and §9.10 smells inside the module this step conforms.

### ApiCompat reports a notnull constraint on the four generic `With*` methods

**Original plan:** the public API diff is empty.

**Actual implementation:** ApiCompat (both directions, strict and default) lists the two compiler-generated
`extension` marker types and CP0021 "notnull" on `WithTopicPublisher`, `WithTopicReceiver`, `WithQueuePublisher`
and `WithQueueReceiver`. The emitted static methods lose the oblivious nullable annotation of `TMessage`; the
non-nullable `IMessage` constraint already implies notnull, so no caller sees a new warning or a binary change.
Recorded as a non-exception in Coding Guide §9.7.
