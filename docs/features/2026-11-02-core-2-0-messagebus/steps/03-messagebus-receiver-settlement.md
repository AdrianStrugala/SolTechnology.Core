---
spec: 2026-11-02-core-2-0-messagebus
step: 03
status: done
---

# Step 03: MessageBus — receiver settlement matches its contract; publish→receive round-trip

## Summary
`MessageBusReceiver`'s XML summary promises dead-lettering on the last delivery attempt, but
handler failures are always abandoned. It invokes handlers through `MethodInfo.Invoke`, so
synchronous throws arrive wrapped in `TargetInvocationException`, which defeats the cancellation
filter at `:139` (`infra#14`, `Receive/MessageBusReceiver.cs:13-20,143-158,179`). Options are not
validated (`infra#21`). No test exercises settlement or `ServiceBusFixture` (`infra#25`).

Reading the entity's `MaxDeliveryCount` from runtime properties would need
`ServiceBusAdministrationClient` and the Manage claim, which Listen-only receivers do not have
today. RQ6 (user, 2026-11-02): a new `MessageBusConfiguration.MaxDeliveryCount` option (default
10, the Service Bus default) is the source instead (additive, `C-27`, P-17).

Former core step 34; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.MessageBus/Configuration/MessageBusConfiguration.cs` — EDIT — NEW `public int MaxDeliveryCount { get; set; } = 10;` with an XML doc: set it to the entity's configured value; no Manage claim needed (`C-27`)
- `src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs` — EDIT — dead-letter with reason `HandlerFailedMaxDelivery` when `args.Message.DeliveryCount >= MaxDeliveryCount` (from options); compiled generic delegate instead of `MethodInfo.Invoke`
- `src/SolTechnology.Core.MessageBus/ModuleInstaller.cs` — EDIT — `:31` `.Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString))` and `.Validate(o => o.MaxDeliveryCount > 0)`
- `tests/SolTechnology.Core.MessageBus.Tests/SolTechnology.Core.MessageBus.Tests.csproj` — EDIT — `ProjectReference` to `src/SolTechnology.Core.ServiceBus.Testing` (first use of `ServiceBusFixture`; testing-kits step 02 reuses it)
- `tests/SolTechnology.Core.MessageBus.Tests/MessageBusReceiverTests.cs` — NEW — uses `ServiceBusFixture`
- `tests/SolTechnology.Core.MessageBus.Tests/ModuleInstallerTests.cs` — EDIT — `:36-123` rename to `Method_Scenario_ExpectedOutcome` and add `// Arrange/Act/Assert`

## Changes
- Tests:
  - `Publish_ThenReceive_HandlerCalledAndMessageCompleted`.
  - `Receive_HandlerThrows_AbandonedThenDeadLetteredOnLastAttempt`.
  - `Receive_HandlerThrowsSync_IsNotWrapped`.
  - `AddSolMessageBus_MaxDeliveryCountZero_HostStartFails`.
- The dead-letter test sets `MaxDeliveryCount = 2` and needs no administration client.
- Premortem RM-13 (core step 00, 2026-11-02, scenario 13): append a `P-nn` row to the core brief. An empty MessageBus `ConnectionString` or `MaxDeliveryCount <= 0` now fails at host start with `OptionsValidationException`.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green (Docker, Service Bus emulator).
- [x] No public or protected signature changed; the only new public member is `MessageBusConfiguration.MaxDeliveryCount` (`C-27`, confirmed by the user on 2026-11-02, RQ6).
- [x] `grep -n "ServiceBusAdministrationClient" src/SolTechnology.Core.MessageBus/Receive -r` prints nothing.
- [x] The core brief's P-17 row matches the delivered behaviour; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [x] RM-13: the core brief's P-table has the MessageBus options-validation row.

## Open questions
- none (RQ6 resolved by the user on 2026-11-02: configuration option, default 10)

## Deviations

### One parameterised options-validation test instead of `AddSolMessageBus_MaxDeliveryCountZero_HostStartFails`

**Original plan:** A single test `AddSolMessageBus_MaxDeliveryCountZero_HostStartFails`.

**Actual implementation:** `AddSolMessageBus_InvalidOptions_HostStartFails` with four `[TestCase]`s (empty and
whitespace `ConnectionString`, `MaxDeliveryCount` 0 and -1) covers both RM-13 rules in one test (Guide §8:
parameterise instead of duplicating).

### Dead-letter reason and description recorded in P-17; P-24 added

**Original plan:** Append a `P-nn` row for RM-13; P-17 states the dead-letter threshold only.

**Actual implementation:** P-24 is the RM-13 row. P-17 (core brief and feature brief) also names the
dead-letter reason `HandlerFailedMaxDelivery` and the handler exception's message as the description,
because dead-letter tooling that filters on the broker's `MaxDeliveryCountExceeded` sees a new reason.

### Options read through the service provider

**Original plan:** `MaxDeliveryCount` "from options".

**Actual implementation:** The receiver resolves `IOptions<MessageBusConfiguration>` from its existing
`IServiceProvider` instead of a new constructor parameter, because `MessageBusReceiver` is still public
in this step and its constructor signature must not change (acceptance criterion 2); step 04 makes it internal.

Verification: the three emulator tests and the four validation cases failed with the production changes
reverted (`HandlerFailedMaxDelivery` expected, abandon observed; the sync throw arrived wrapped) and pass with them;
`dotnet test tests/SolTechnology.Core.MessageBus.Tests` 22/22 green against the Service Bus emulator (arm64, about 30 s).

