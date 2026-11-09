---
status: completed
created: 2026-11-02
completed: 2026-11-09
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

Steps lived in the temporary working folder `2026-11-02-core-2-0-messagebus`, which the retrospective deleted on
2026-11-09. Gates: `review` was `done (2026-11-02)`; `premortem` was `skipped` — the core premortem verdict
`go-with-mitigations (2026-11-02)` covered these steps.

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

- [x] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations`.
- [x] Regression tests for `B1`, `B2`, and `infra#13` (MessageBus) fail on `4097d47f` and pass; the MessageBus resolution test has no `[Ignore]`.
- [x] `C-14` and `C-27` shipped; the publish→receive round-trip test (step 03) is green against the ServiceBus emulator.
- [x] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green.
- [x] The module resolution test uses `ValidateOnBuild = true` and `ValidateScopes = true`, and no resolution test is `[Ignore]`d.
- [x] `find src/SolTechnology.Core.MessageBus -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] Every packed `.nupkg` of this feature contains `lib/net10.0/*.xml`.
- [x] Every C# snippet in `docs/Bus.md` compiles in a throwaway console project against the 2.0 API, as recorded in the module-docs step.
- [x] The library-scoped `[{Placeholder}]` template check of the conformance or log-template step prints nothing.

## Completion summary

Delivered on `master` between `ba8acbc5` and `ce492389`, plus the concurrent-creation fix `57c060e5`; the code review
fixes (`c5af0f48` to `7c1d99ee`) and the retrospective ran on 2026-11-09. The user asked on 2026-11-09 to fix the
review findings, run the retrospective, and close the feature without further confirmation (`CLAUDE.md §2`).

- **`B1`, `B2`, `infra#13`, A5 (steps 01, 02):** the broker is registered with a factory lambda and keeps its
  constructor internal; the module registers its own keyed `ServiceBusClient`, which the health check and the broker
  use, and the correlation service. The resolution tests run with `ValidateOnBuild` and `ValidateScopes` and no
  `[Ignore]`.
- **`C-27`, P-17, P-24, RM-13 (step 03):** the receiver completes a handled message, abandons a failing one, and
  dead-letters it with reason `HandlerFailedMaxDelivery` on delivery `MaxDeliveryCount` (default 10); unreadable bodies
  are dead-lettered at once. An empty `ConnectionString` or a `MaxDeliveryCount` of zero or less stops the host. A
  publish→receive round trip runs against the Service Bus emulator.
- **`C-14` (step 04):** `MessageBusBroker`, `IMessageBusBroker`, `MessagePublisher`, and `MessageBusReceiver` are
  internal.
- **Concurrent provisioning (coordinator, 2026-11-09, `57c060e5`):** with `CreateResources` on, an entity that another
  instance creates between the existence check and the create counts as created.
- **Docs (steps 05, 06):** the package ships XML docs, and `docs/Bus.md` was rewritten against the 2.0 API with every
  C# block compiled, including the "Running several instances" section. The code review changed only table rows.
- **Conformance and logs (steps 07, 08, L-03):** the broker, receiver, and installer were split into collaborators
  under 150 lines, with `extension` blocks, braces, and one-line comments; every MessageBus log template brackets its
  placeholders.
- **Code review fixes (2026-11-09):** settlement no longer uses the processor's cancellation token, so a message handled
  during shutdown completes; created queues and subscriptions get `MaxDeliveryCount`; a completion failure is a Warning
  and never an abandon or dead-letter; a failed initialisation can be retried without duplicate senders. The receiver
  tests were split under 150 lines (see the last section).
- **Architecture:** [`docs/architecture/infrastructure-modules.md`](../architecture/infrastructure-modules.md) has the
  MessageBus section: public surface, registration with the keyed client and the A5 rationale, all-or-nothing
  initialisation, provisioning with `MaxDeliveryCount` and concurrent creation, settlement including graceful shutdown,
  multi-instance semantics (competing consumers, at-least-once delivery), and the test setup.
  `docs/architecture/background-processing.md` does not name MessageBus and needed no change. The premortem reference
  `.github/skills/premortem/references/messagebus.md` now names the concrete checks for duplicate processing and
  `MaxDeliveryCount`, and a new item for all-or-nothing initialisation.
- **Final verification (2026-11-09, `7c1d99ee` plus this record):** `dotnet build SolTechnology.Core.slnx -c Release`
  reported 0 errors and the NU190x / NU1605 grep printed nothing; the file-size check and the multi-line placeholder
  scan of step 08 printed nothing; the Release pack of `SolTechnology.Core.MessageBus` contains
  `lib/net10.0/SolTechnology.Core.MessageBus.xml`; and `dotnet test SolTechnology.Core.slnx -c Release` passed 18
  projects (872 passed, 11 skipped, 0 failed), including MessageBus (39) and the DreamTravel component and unit tests.

Changes to the core brief ([`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md)) for
release 05:

- A new "Release-notes inputs" entry (messagebus code review, 2026-11-09) covers the four behaviour changes of the
  review fixes, including the new Warning template `Failed to complete message [{MessageId}]; lock will expire
  naturally`. The entry for concurrent resource creation was added earlier on 2026-11-09.
- P-17 and P-24 were extended or added in step 03 (see Deviations); `C-14`, `C-27`, and L-03 shipped as written. The
  new Warning template is additional and already bracketed, so L-03 needs no change.

## Deviations

### P-17 extended and P-24 added (step 03)

**Original plan:** append a `P-nn` row for RM-13; P-17 names the dead-letter threshold only.

**Actual implementation:** P-24 is the RM-13 row. P-17 also names the dead-letter reason `HandlerFailedMaxDelivery` and
the handler exception's message as the description, because dead-letter tooling that filters on the broker's
`MaxDeliveryCountExceeded` sees a new reason.

### Client options built in an internal method (step 02)

**Original plan:** `MessageBusClientFactory.Create` applies the transport and retry options, and a test reads them from
the client.

**Actual implementation:** `ServiceBusClient` does not expose its retry options, so an internal `CreateOptions` builds
them and the test checks it; the empty-`ConnectionString` `ArgumentException` moved from the broker constructor into
`Create`, because the client is built before the broker.

### "Running several instances" and concurrent creation (coordinator, 2026-11-09)

**Original plan:** not part of the feature.

**Actual implementation:** after step 04, `57c060e5` made `CreateResources` tolerate `MessagingEntityAlreadyExists`, and
step 06 added the "Running several instances" section to `docs/Bus.md`: competing consumers, a per-instance subscription
for fan-out (not provided), at-least-once delivery, no ordering across instances.

### Collaborators extracted beyond the plan (step 07)

**Original plan:** split the broker below 150 lines and move the endpoint helpers out of `ModuleInstaller.cs`.

**Actual implementation:** the broker delegates to `ServiceBusEndpoints` and `ServiceBusResourceProvisioner`, the
receiver to `MessageDeserializer`, `MessageSettler`, and `HandlerInvoker`, and the helpers live in `EndpointRegistrar`;
the health-check extension also moved into an `extension` block. ApiCompat reports CP0021 "notnull" on the four generic
`With*` methods, accepted under Coding Guide §9.7 (see the last section).

### Template check and pinning tests (step 08)

**Original plan:** a single-line `grep` for unbracketed placeholders.

**Actual implementation:** every template sits on the line after its `Log*(` call, so the check is a multi-line scan of
each call; new tests pin the settlement templates and, on the emulator, the start, publish, handled, stop-failure,
publish-failure, and processor-error templates.

### Code review fixes after step 08 (2026-11-09)

**Original plan:** the retrospective follows step 08 directly.

**Actual implementation:** four review findings were fixed first, each with a test that fails without it (see the last
section). Settlement ignoring the processor's token, `MaxDeliveryCount` on created entities, and the completion-failure
Warning change observable behaviour; the core brief has the release-notes input.

## Follow-ups

- `SolTechnology.Core.ServiceBus.Testing` leaves the emulator's MSSQL sidecar running after `ServiceBusFixture` is
  disposed when Ryuk is disabled, which `TestContainersContext` does. During this work, sidecars from earlier test runs
  piled up and the next emulator starts failed until they were removed by hand. Owner: the testing-kits feature.
- `tests/SolTechnology.Core.MessageBus.Tests/ModuleInstallerTests.cs` has 222 lines, over the Coding Guide §9 cap;
  split it like the receiver tests.
- When dead-lettering an unreadable message fails, the exception enters the handler-failure path, so it is logged as
  `Handler failure …` at Error and abandoned instead of logged as a dead-letter failure at Warning. Low impact: the
  message comes back and is dead-lettered again.

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
