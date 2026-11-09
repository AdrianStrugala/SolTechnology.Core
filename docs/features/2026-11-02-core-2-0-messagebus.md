---
status: in-progress
created: 2026-11-02
completed:
---

# SolTechnology.Core 2.0 — MessageBus

> Historical delivery record. It may not describe the current system.

## Goal

Deliver the 2.0 hardening of `SolTechnology.Core.MessageBus` end-to-end, as one library of the SolTechnology.Core 2.0
flagship release. Success means:

- `IMessageBusBroker` resolves from DI, the health check gets a registered `ServiceBusClient`, and the correlation service is registered (`B1`, `B2`, `infra#13`, A5).
- The receiver settles messages as documented and dead-letters on the last attempt using the new `MessageBusConfiguration.MaxDeliveryCount` (`C-27`, P-17, RQ6); a publish→receive round-trip test covers it.
- `MessageBusBroker`, `IMessageBusBroker`, `MessagePublisher`, and `MessageBusReceiver` are internal (`C-14`).
- MessageBus ships XML docs, a `docs/Bus.md` README that compiles against 2.0, conforms to the Coding Guide, and logs with `[{Placeholder}]` templates.

## Context

**Depends on:**

- Core 02, 03, and 04 of [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) are `done` (CI fails on any test failure, tag-only publishing, version 2.0.0 on `master`).
- [`2026-11-02-core-2-0-logging`](2026-11-02-core-2-0-logging.md) is `completed`.

Delivery order position 11 of 13 in the umbrella brief's "Per-library regroup" section. The
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md) feature
depends on this feature being `completed`.

### Origin

Regrouped on 2026-11-02 from [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) (user: "podziel implementację feature 1 na
każdą bibliotekę osobno"). Each step names its former core step in a "Former core step" line; the
umbrella brief's "Per-library regroup" table maps every former number. The evidence is the
full-repository code review of 2026-11-02 against `master@4097d47f`; finding IDs follow the
umbrella brief's "Evidence" table. Findings acted on here: `B1`, `B2`, `P1-2`, `P1-4`, `cqrs-tale#16`, `infra#13`, `infra#14`, `infra#18-19`, `infra#20`, `infra#21`, `infra#24`, `infra#25`.

### Decisions owned by this feature

The umbrella brief keeps the shared decision tables and the user's confirmations (`C-01`…`C-24`,
`C-26`, `C-27` confirmed, `C-25` rejected, 2026-11-02). The rows below are copied from it. Step
numbers in copied `A`, `AR`, `Q`, and `RQ` rows are former core numbers.

Public API changes (`CLAUDE.md §2`, confirmed by the user on 2026-11-02):

| ID | Package | Exact change | Step |
|---|---|---|---|
| C-14 | MessageBus | `MessageBusBroker`, `IMessageBusBroker` (with `EnsureInitializedAsync`), `MessagePublisher`, `MessageBusReceiver` → `internal` | messagebus 04 |
| C-27 | MessageBus (additive) | NEW `MessageBusConfiguration.MaxDeliveryCount` (`int`, default 10) used by the receiver to dead-letter on the last attempt (RQ6) | messagebus 03 |

Behavioural and persisted-contract breaks:

| ID | Change | Consumer impact | Step |
|---|---|---|---|
| P-17 | The receiver dead-letters a failing message when `DeliveryCount >= MessageBusConfiguration.MaxDeliveryCount` (default 10) instead of always abandoning; the dead-letter reason is `HandlerFailedMaxDelivery` and the description is the handler exception's message (instead of the broker's `MaxDeliveryCountExceeded`) | Set `MaxDeliveryCount` to the entity's value; a lower value dead-letters earlier than the broker would; dead-letter tooling that filters on `MaxDeliveryCountExceeded` must also accept `HandlerFailedMaxDelivery` | messagebus 03 |
| P-24 | `MessageBusConfiguration.ConnectionString` is validated non-empty and `MessageBusConfiguration.MaxDeliveryCount` greater than zero at start (premortem RM-13) | An empty or whitespace `ConnectionString`, which 1.x reported as `ArgumentException` from the broker constructor, and a `MaxDeliveryCount` of zero or less now stop the host with `OptionsValidationException` | messagebus 03 |

Log contract changes:

- **L-03** (MessageBus part): the receiver and publisher (`infra#20`). Their 11 templates now bracket `MessageType`,
  `MessageId`, `Entity`, `DeliveryCount`, `Source`, `Namespace` and `Message`; no property is renamed, so only queries
  that match the message text change. Step 08.

Design alternatives (blue-red-team, 2026-11-02):

| # | Choice | Alternatives | Recommendation and crux |
|---|---|---|---|
| A5 | `IMessageBusBroker` resolution (`B1`) | (a) make the constructor public; (b) register with a factory lambda and keep the constructor internal | **(b) in former step 12 (messagebus 02)**, then make the types internal in former step 48 (messagebus 04, `C-14`). Adds no public surface that 2.0 would remove again. |

User decisions (2026-11-02):

| ID | Question | Decision (user, 2026-11-02) | Steps |
|---|---|---|---|
| RQ6 | Source of `MaxDeliveryCount` | **(a)** New `MessageBusConfiguration.MaxDeliveryCount`, default 10, additive (`C-27`, P-17). | 34 |

Premortem mitigations carried verbatim from core 00: RM-13 (step 03), RM-16 (step 07).

Additional requirement (coordinator, 2026-11-09, after the user asked whether MessageBus handles several instances):
`MessageBusBroker` provisions entities with check-then-create, so with `CreateResources = true` an instance that
loses the creation race to another instance got `ServiceBusException` (`MessagingEntityAlreadyExists`, HTTP 409) and
failed to start. A separate commit after step 04 (`fix(messagebus): tolerate concurrent resource creation`) treats
that error as success and logs it at Debug; step 06 adds a "Running several instances" section to `docs/Bus.md`. The
core brief has the matching release-notes input. Non-breaking (`Semver: PATCH`).

MessageBus.Tests gains a `ProjectReference` to `SolTechnology.Core.ServiceBus.Testing` (step 03); testing-kits 02 reuses it. The ServiceBus.Testing kit itself is the testing-kits feature.

## Scope

### In scope

- Source: `src/SolTechnology.Core.MessageBus`, and the matching test projects.
- Module docs: `docs/Bus.md`.
- Phases, in order: correctness and broken-module fixes → 2.0 breaking API cleanup → XML docs and
  module docs → Coding Guide conformance and `[{Placeholder}]` log templates → retrospective.

### Out of scope

- Changing the queue lookup from `typeof(TMessage).Name` to `FullName` (RM-16 forbids it).
- Work owned by the foundation feature (CI, publishing, version, DreamTravel exception page, secret
  artifacts, zero-dependency base package, central `GenerateDocumentationFile`), the other library
  features, the showcase feature, and the release feature.

## Implementation plan

Steps live in the temporary working folder `2026-11-02-core-2-0-messagebus/` (see its `summary.md`). Gates: `review` is
`done (2026-11-02)`; `premortem` is `skipped` — the core premortem verdict `go-with-mitigations (2026-11-02)`
covers these steps.

Plan review (2026-11-02): the regrouping was checked against the pre-regroup plan (`dddac21c`) and
found complete. User answers recorded, not re-asked: the delivery order with AUID before Tale is
confirmed (Tale references AUID); the per-library split, the premortem reuse, and the `C-01`…`C-24`,
`C-26`, `C-27` confirmations stand (`C-25` rejected). Review fixes applied here: the retrospective's DreamTravel test command is runnable.

Notation: an unqualified step number is a step of this feature; `core NN` names step `NN` of
[`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md); other short names (`result`, `logging`, `cqrs`, `auid`, `tale`, `http`, `api-auth`,
`cache`, `sql`, `blob`, `messagebus`, `hangfire`, `testing-kits`) name the library features listed
in the umbrella brief; `release NN` names a step of
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md).

| Step | Title | Former core step(s) |
|---|---|---|
| 01 | DI resolution test — MessageBus | 11 |
| 02 | MessageBus — resolvable broker, registered health-check client, correlation service | 12 |
| 03 | MessageBus — receiver settlement matches its contract; publish→receive round-trip | 34 |
| 04 | MessageBus — internal implementation types | 48 |
| 05 | XML docs shipped — MessageBus | 57 |
| 06 | Module docs — `docs/Bus.md` | 61 |
| 07 | Guide conformance — MessageBus | 68 |
| 08 | Log templates — `[{Placeholder}]` in MessageBus (L-03) | 70 |
| 09 | Retrospective | 71 |

Steps run in number order; each is one PR.

## Acceptance criteria

- [ ] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations`.
- [ ] Regression tests for `B1`, `B2`, and `infra#13` (MessageBus) fail on `4097d47f` and pass; the MessageBus resolution test has no `[Ignore]`.
- [ ] `C-14` and `C-27` shipped; the publish→receive round-trip test (step 03) is green against the ServiceBus emulator.
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green.
- [ ] The module resolution test uses `ValidateOnBuild = true` and `ValidateScopes = true`, and no resolution test is `[Ignore]`d.
- [ ] `find src/SolTechnology.Core.MessageBus -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] Every packed `.nupkg` of this feature contains `lib/net10.0/*.xml`.
- [ ] Every C# snippet in `docs/Bus.md` compiles in a throwaway console project against the 2.0 API, as recorded in the module-docs step.
- [ ] The library-scoped `[{Placeholder}]` template check of the conformance or log-template step prints nothing.

## Completion summary

## Deviations

## Follow-ups

## Code review fixes (2026-11-09)

A code review of `ba8acbc5~1..ce492389` found four defects. Each fix has a test that fails without it; release-notes
inputs are in the core brief's "Release-notes inputs" section.

| # | Finding | Fix | Test |
|---|---|---|---|
| 1 | Major. `MessageSettler` completed, abandoned and dead-lettered with `args.CancellationToken`, which the SDK cancels when `StopProcessingAsync` starts, so a handler that succeeded during shutdown could not complete its message; another instance handled it again on every rolling deploy. | Settlement uses `CancellationToken.None`; the handler still gets the processor's token. `docs/Bus.md` describes the shutdown row. | `MessageBusReceiverSettlementTests.Stop_HandlerFinishesAfterShutdownStarts_MessageCompletedNotRedelivered` (emulator, new queue `receiver-shutdown`): the handler ignores the token and returns after `StopAsync` has cancelled it; the queue and its dead-letter queue are empty. |
| 2 | Major. With `CreateResources = true`, `ServiceBusResourceProvisioner` created queues and subscriptions with the broker default `MaxDeliveryCount` of 10 instead of `MessageBusConfiguration.MaxDeliveryCount`. | Created with `CreateQueueOptions` and `CreateSubscriptionOptions` carrying the configured value. | `MessageBusBrokerTests.EnsureInitialized_CreateResources_CreatesQueueAndSubscriptionWithConfiguredMaxDeliveryCount` reads the options passed to a substituted administration client (the emulator has no administration API). |
| 3 | Minor. `Complete` ran inside the handler's `try`, so a completion failure was logged as a handler failure and the message was abandoned, or dead-lettered on its last delivery. | `MessageBusReceiver` completes after the handler block; `MessageSettler.Complete` logs the new Warning `Failed to complete message [{MessageId}]; lock will expire naturally` and never re-settles. | `MessageBusReceiverCompletionTests.HandleMessage_HandlerSucceedsButCompleteFails_LogsWarningAndDoesNotSettleAsFailure` drives a processor whose completion throws, with `MaxDeliveryCount = 1`; a new `MessageSettlerTests` case pins the template. |
| 4 | Minor. `EnsureInitializedAsync` added senders and processors one endpoint at a time, so a failure halfway left them in place; a retry added a second sender (duplicate publishes) and receiver registration threw. | `ServiceBusEndpoints` provisions every entity first, builds senders and processors into local collections, keeps them only when all were created, and disposes them otherwise. | `MessageBusBrokerTests.EnsureInitialized_RetryAfterProvisioningFailedHalfway_CreatesEachSenderAndProcessorOnce` fails the first subscription create, retries, and expects one sender and two processors. |

Also from the review:

- **CP0021 accepted.** ApiCompat's CP0021 "notnull" on `WithQueuePublisher`, `WithQueueReceiver`, `WithTopicPublisher`
  and `WithTopicReceiver` is accepted as Coding Guide §9.7 records it: the non-nullable `IMessage` constraint already
  implies notnull, so no caller sees a new warning or a binary change. The repository has no ApiCompat suppression
  file, so this brief is the record.
- **Receiver tests split.** `MessageBusReceiverTests` (244 lines) became `ReceiverTestHost` (the emulator host, handler
  probe and polling helpers), `MessageBusReceiverSettlementTests` and `MessageBusReceiverLogTemplateTests`, each under
  150 lines, with test names and assertions unchanged.
