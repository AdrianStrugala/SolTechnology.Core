---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 34
status: to-do
---

# Step 34: MessageBus — receiver settlement matches its contract; publish→receive round-trip

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

## Affected components
- `src/SolTechnology.Core.MessageBus/Configuration/MessageBusConfiguration.cs` — EDIT — NEW `public int MaxDeliveryCount { get; set; } = 10;` with an XML doc: set it to the entity's configured value; no Manage claim needed (`C-27`)
- `src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs` — EDIT — dead-letter with reason `HandlerFailedMaxDelivery` when `args.Message.DeliveryCount >= MaxDeliveryCount` (from options); compiled generic delegate instead of `MethodInfo.Invoke`
- `src/SolTechnology.Core.MessageBus/ModuleInstaller.cs` — EDIT — `:31` `.Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString))` and `.Validate(o => o.MaxDeliveryCount > 0)`
- `tests/SolTechnology.Core.MessageBus.Tests/SolTechnology.Core.MessageBus.Tests.csproj` — EDIT — `ProjectReference` to `src/SolTechnology.Core.ServiceBus.Testing` (first use of `ServiceBusFixture`; step 37 reuses it)
- `tests/SolTechnology.Core.MessageBus.Tests/MessageBusReceiverTests.cs` — NEW — uses `ServiceBusFixture`
- `tests/SolTechnology.Core.MessageBus.Tests/ModuleInstallerTests.cs` — EDIT — `:36-123` rename to `Method_Scenario_ExpectedOutcome` and add `// Arrange/Act/Assert`

## Changes
- Tests:
  - `Publish_ThenReceive_HandlerCalledAndMessageCompleted`.
  - `Receive_HandlerThrows_AbandonedThenDeadLetteredOnLastAttempt`.
  - `Receive_HandlerThrowsSync_IsNotWrapped`.
  - `AddSolMessageBus_MaxDeliveryCountZero_HostStartFails`.
- The dead-letter test sets `MaxDeliveryCount = 2` and needs no administration client.
- Premortem RM-13 (step 00, 2026-11-02, scenario 13): append a `P-nn` row to the brief. An empty MessageBus `ConnectionString` or `MaxDeliveryCount <= 0` now fails at host start with `OptionsValidationException`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green (Docker, Service Bus emulator).
- [ ] No public or protected signature changed; the only new public member is `MessageBusConfiguration.MaxDeliveryCount` (`C-27`, confirmed by the user on 2026-11-02, RQ6).
- [ ] `grep -n "ServiceBusAdministrationClient" src/SolTechnology.Core.MessageBus/Receive -r` prints nothing.
- [ ] The brief's P-17 row matches the delivered behaviour; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [ ] RM-13: the brief's P-table has the MessageBus options-validation row.

## Open questions
- none (RQ6 resolved by the user on 2026-11-02: configuration option, default 10)

## Deviations
