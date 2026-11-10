---
spec: 2026-11-02-logging-chronicle
step: 10
status: done
---

# Step 10: CQRS auto-steps — pipeline requests, event handlers, fire-and-forget event roots

## Summary
`LoggingPipelineBehavior` records every command and query as a Chronicle step through `Begin`, so
a request inside an HTTP root becomes a child step and a command in a worker becomes its own root
(B9). The Chronicle step now produces the `OperationsName` Activity (B7), so the CQRS-owned
`ActivitySource` added by the 2.0 logging work is removed and there is no duplicate span.
`EventDispatcher` records one step per event handler, and the default `InMemoryEventPublisher`
wraps each fire-and-forget dispatch in `BeginRoot` (B11, CP-06). A handler cancelled while the
dispatch's token is cancelled is `Cancelled`, never `Failed`, because the 2.0 dispatcher stops
and rethrows in that case (P-26). The lifecycle entries 2137–2139 and the failed-`Result` entry
2149 stay unchanged.

## Affected components
- `src/SolTechnology.Core.CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs` — EDIT — primary constructor `(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger, IChronicle chronicle)` (CH-10); `using var step = chronicle.Begin(operationName)` replaces `OperationsActivitySource.Instance.StartActivity(operationName, ActivityKind.Internal)`; the `activity?.SetTag`/`SetStatus`/`AddException` calls become `step.Set`/`Fail`/`Cancel`
- `src/SolTechnology.Core.CQRS/PipelineBehaviors/OperationsActivitySource.cs` — DELETE — the internal holder of the CQRS-owned `ActivitySource` (`Instance`, named `CoreLoggingActivitySources.OperationsName`); its only user is `LoggingPipelineBehavior`
- `src/SolTechnology.Core.CQRS/Internal/EventDispatcher.cs` — EDIT — primary-constructor parameter `IChronicle? chronicle = null`; one `Step($"Event {eventType.Name} {handler.GetType().Name}")` per handler inside the existing `try`
- `src/SolTechnology.Core.CQRS/Internal/InMemoryEventPublisher.cs` — EDIT — primary-constructor parameter `IChronicle? chronicle = null`; inside `Task.Run` (`DispatchFireAndForget`), `BeginRoot($"Event {@event.GetType().Name}")` around scope creation and dispatch; the existing `catch` that logs "Failed to dispatch event" also calls `Fail(exception)` on the root
- `src/SolTechnology.Core.CQRS/ModuleInstaller.cs` — EDIT — when `options.UseLogging`: `services.AddSolChronicle()` (CP-03)
- `src/SolTechnology.Core.CQRS/CQRSOptions.cs` — EDIT — XML doc of `UseLogging` names Chronicle steps
- `tests/SolTechnology.Core.CQRS.Tests/SolTechnology.Core.CQRS.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (version from `Directory.Packages.props`, added by step 04)
- `tests/SolTechnology.Core.CQRS.Tests/PipelineBehaviors/LoggingPipelineBehaviorChronicleTests.cs` — NEW
- `tests/SolTechnology.Core.CQRS.Tests/Notifications/EventChronicleTests.cs` — NEW — next to `EventDispatcherCancellationTests.cs` (P-26), whose assertions stay unchanged
- `tests/SolTechnology.Core.CQRS.Tests/Registration/AddSolCQRSTests.cs` — EDIT

## Changes
- Pipeline step:
  - Name: `typeof(TRequest).Name` (unchanged operation name).
  - `[LogScope]` bindings → `step.Set(key, scopeValue)`, where `scopeValue` is the already masked value; the raw value of a `[Masked]` property is never passed.
  - A failed `Result` (detection added by `2026-11-02-core-2-0-cqrs` step "CQRS — log failed `Result`s as failures") → `step.Fail(error.GetType().Name, error.Message)` (RM-15: type and message only).
  - Exception → `step.Fail(exception)`, then rethrow.
  - `OperationCanceledException` with `cancellationToken.IsCancellationRequested` → `step.Cancel()`, then rethrow.
- Event dispatch (decision R3):
  - Handler exceptions keep today's per-handler isolation and Error log; the handler step is `Failed`.
  - An `OperationCanceledException` thrown while the dispatch's `cancellationToken` is cancelled
    (P-26, `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)`)
    → `step.Cancel()`, then the dispatcher rethrows as today. The remaining handlers are not called
    and get no step. An `OperationCanceledException` while the token is not cancelled (for example
    an `HttpClient` timeout) stays a handler failure → `Failed`.
  - The root around the dispatch follows the token's owner: `InMemoryEventPublisher` dispatches with
    `CancellationToken.None`, so its `Event {EventType}` root is never `Cancelled`; the Hangfire
    `Event {EventType}` root (step 14) is `Cancelled` when the server's shutdown token stopped it.
  - `IChronicle` is optional (`= null` default) so `UseLogging = false` hosts without Chronicle keep working; the types stay internal, so no `§2` gate applies.
- Tests (`FakeLogCollector`; reports asserted on the rendered tree in `FakeLogRecord.Message`, because `FakeLogger` stringifies state values):
  - `Handle_OutsideRoot_EmitsOne2154EntryNamedAfterRequest`.
  - `Handle_InsideRoot_AddsChildStepNamedAfterRequest`.
  - `Handle_NestedSend_BuildsNestedSteps`.
  - `Handle_FailedResult_StepFailedWithTypeAndMessageOnly`.
  - `Handle_Throws_StepFailedAndRethrows`.
  - `Handle_CallerCancelled_StepCancelled`.
  - `Handle_MaskedLogScopeProperty_AttributeHoldsMaskedValueOnly`.
  - `Handle_WithOperationsListener_StartsExactlyOneActivity`.
  - `Handle_LifecycleEntries2137To2139_Unchanged`.
  - `Publish_InsideOpenRoot_EventGetsOwnRootAndOuterRootHasNoEventStep`.
  - `Dispatch_HandlerFails_StepFailedAndOtherHandlersRun`.
  - `Dispatch_HandlerCancelledWithCancelledToken_StepCancelledRethrowsAndLaterHandlersHaveNoStep` (P-26).
  - `Dispatch_HandlerThrowsCanceledWithoutCancelledToken_StepFailedAndOtherHandlersRun`.
  - `AddSolCQRS_Default_RegistersChronicle`.
  - `AddSolCQRS_UseLoggingFalse_DoesNotRegisterChronicle`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.CQRS.Tests` is green.
- [ ] `grep -rn "new ActivitySource\|StartActivity" src/SolTechnology.Core.CQRS --include=*.cs` prints nothing, and `src/SolTechnology.Core.CQRS/PipelineBehaviors/OperationsActivitySource.cs` no longer exists.
- [ ] `dotnet test tests/SolTechnology.Core.CQRS.Tests --filter "FullyQualifiedName~EventDispatcherCancellation"` is green unchanged (P-26).
- [ ] `dotnet build SolTechnology.Core.slnx` is green, and `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.

## Open questions
- none (OQ-1 confirmed by the user 2026-11-02: `CH-10` `LoggingPipelineBehavior` constructor; `CH-11` consumed from step 08)

## Deviations

### A successful operation activity keeps status `Unset`

**Original plan:** the `activity?.SetTag`/`SetStatus`/`AddException` calls become
`step.Set`/`Fail`/`Cancel`.

**Actual implementation:** as planned, so the behavior no longer calls
`SetStatus(ActivityStatusCode.Ok)` on success: `ChronicleStep` has no "ok" member and the
OpenTelemetry guidance leaves a successful instrumentation span `Unset`. A failed `Result` now also
carries its message as the status description and the `error.type` tag (step 05). The two existing
assertions in `LoggingPipelineBehaviorTests` expect `Unset`. A caller cancellation still writes the
unchanged 2139 entry; only the step is `Cancelled`.

### Test layout

- `ChronicleTestHost` (test project root) builds the real recorder from `AddSolChronicle` with a
  `FakeLogCollector`; the existing pipeline tests use it too.
- The pipeline tests are split into `LoggingPipelineBehaviorChronicleTests` and
  `LoggingPipelineBehaviorChronicleOutcomeTests` over `LoggingPipelineBehaviorChronicleTestBase`
  (150-line budget); `Handle_LifecycleEntries2137To2139_Unchanged` covers success and exception.
- `Dispatch_HandlerFails_StepFailedAndOtherHandlersRun` and
  `Dispatch_HandlerThrowsCanceledWithoutCancelledToken_StepFailedAndOtherHandlersRun` are one test
  parameterized on the handler type.
- `Publish_InsideOpenRoot_...` does not assert `DetachedFrom`: whether the publishing root is still
  open when `Task.Run` starts is a race (step 08 deviation).
- The registration tests live in `Registration/AddSolCQRSChronicleTests.cs`, which keeps
  `AddSolCQRSTests.cs` within its size.

