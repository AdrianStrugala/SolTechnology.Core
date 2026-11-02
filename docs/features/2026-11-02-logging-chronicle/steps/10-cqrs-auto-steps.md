---
spec: 2026-11-02-logging-chronicle
step: 10
status: blocked
---

# Step 10: CQRS auto-steps — pipeline requests, event handlers, fire-and-forget event roots

## Summary
`LoggingPipelineBehavior` records every command and query as a Chronicle step through `Begin`, so
a request inside an HTTP root becomes a child step and a command in a worker becomes its own root
(B9). The Chronicle step now produces the `OperationsName` Activity (B7), so the CQRS-owned
`ActivitySource` added by the 2.0 logging work is removed and there is no duplicate span.
`EventDispatcher` records one step per event handler, and the default `InMemoryEventPublisher`
wraps each fire-and-forget dispatch in `BeginRoot` (B11, CP-06). The lifecycle entries 2137–2139
stay unchanged.

## Affected components
- `src/SolTechnology.Core.CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs` — EDIT — primary constructor `(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger, IChronicle chronicle)` (CH-10); `using var step = chronicle.Begin(operationName)` replaces `OperationsSource.StartActivity`; remove `private static readonly ActivitySource OperationsSource`
- `src/SolTechnology.Core.CQRS/Internal/EventDispatcher.cs` — EDIT — constructor parameter `IChronicle? chronicle = null`; one `Step($"Event {eventType.Name} {handler.GetType().Name}")` per handler
- `src/SolTechnology.Core.CQRS/Internal/InMemoryEventPublisher.cs` — EDIT — constructor parameter `IChronicle? chronicle = null`; inside `Task.Run`, `BeginRoot($"Event {@event.GetType().Name}")` around scope creation and dispatch
- `src/SolTechnology.Core.CQRS/ModuleInstaller.cs` — EDIT — when `options.UseLogging`: `services.AddSolChronicle()` (CP-03)
- `src/SolTechnology.Core.CQRS/CQRSOptions.cs` — EDIT — XML doc of `UseLogging` names Chronicle steps
- `tests/SolTechnology.Core.CQRS.Tests/SolTechnology.Core.CQRS.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (same version as step 04)
- `tests/SolTechnology.Core.CQRS.Tests/PipelineBehaviors/LoggingPipelineBehaviorChronicleTests.cs` — NEW
- `tests/SolTechnology.Core.CQRS.Tests/Notifications/EventChronicleTests.cs` — NEW
- `tests/SolTechnology.Core.CQRS.Tests/Registration/AddSolCQRSTests.cs` — EDIT

## Changes
- Pipeline step:
  - Name: `typeof(TRequest).Name` (unchanged operation name).
  - `[LogScope]` bindings → `step.Set(key, scopeValue)`, where `scopeValue` is the already masked value; the raw value of a `[Masked]` property is never passed.
  - A failed `Result` (detection added by `2026-11-02-core-2-0-cqrs` step "CQRS — log failed `Result`s as failures") → `step.Fail(error.GetType().Name, error.Message)` (RM-15: type and message only).
  - Exception → `step.Fail(exception)`, then rethrow.
  - `OperationCanceledException` with `cancellationToken.IsCancellationRequested` → `step.Cancel()`, then rethrow.
- Event dispatch:
  - Handler exceptions keep today's per-handler isolation and Error log; the handler step is `Failed`.
  - `IChronicle` is optional (`= null` default) so `UseLogging = false` hosts without Chronicle keep working; the types stay internal, so no `§2` gate applies.
- Tests (`FakeLogCollector`; reports read from the `@Chronicle` state value):
  - `Handle_OutsideRoot_EmitsOne2150EntryNamedAfterRequest`.
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
  - `AddSolCQRS_Default_RegistersChronicle`.
  - `AddSolCQRS_UseLoggingFalse_DoesNotRegisterChronicle`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.CQRS.Tests` is green.
- [ ] `grep -rn "new ActivitySource\|StartActivity" src/SolTechnology.Core.CQRS --include=*.cs` prints nothing.
- [ ] `dotnet build SolTechnology.Core.slnx` is green, and `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.

## Open questions
- OQ-1 (brief): `CLAUDE.md §2` confirmation of `CH-10` (`LoggingPipelineBehavior` constructor); this step also consumes `CH-11` (step 08).

## Deviations
