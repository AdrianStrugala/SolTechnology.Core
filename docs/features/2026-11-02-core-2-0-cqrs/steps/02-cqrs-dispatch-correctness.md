---
spec: 2026-11-02-core-2-0-cqrs
step: 02
status: done
---

# Step 02: CQRS — descriptive missing-handler errors and interface-based dispatch

## Summary
Dispatch has three reflection defects:

- An unregistered handler, including an `internal` one skipped by the `GetExportedTypes` scan,
  surfaces as a bare `NullReferenceException`, because `GetService` returns null
  (`cqrs-tale#10`, `CQRS/Internal/RequestDispatcherCache.cs:29-37`).
- Behaviours and event handlers are found with `GetType().GetMethod("Handle")`, which returns
  null for explicit interface implementations and throws ambiguous-match for overloads.
  `MethodInfo.Invoke` also wraps synchronous throws (`Mediator.cs:71`,
  `EventDispatcher.cs:23`).
- The dispatcher cache key ignores the handler interface (`cqrs-tale#20`).

Former core step 23; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.CQRS/Internal/RequestDispatcherCache.cs` — EDIT — key by `(requestType, handlerInterfaceType)`; `GetRequiredService` with a message naming the request type and the `RegisterCommandsFromAssembly`/`RegisterQueriesFromAssembly` hint
- `src/SolTechnology.Core.CQRS/Internal/Mediator.cs` — EDIT — `:52-75` invoke behaviours through the interface `MethodInfo` via cached compiled delegates
- `src/SolTechnology.Core.CQRS/Internal/EventDispatcher.cs` — EDIT — `:23` same
- `tests/SolTechnology.Core.CQRS.Tests/MediatorTests.cs` — EDIT

## Changes
- Tests:
  - `Send_UnregisteredHandler_ThrowsInvalidOperationWithRequestName`.
  - `Send_BehaviorWithExplicitInterfaceImpl_IsInvoked`.
  - `Send_HandlerThrowsSynchronously_ExceptionIsNotWrapped`.
  - `Send_TypeImplementingQueryAndCommand_DispatchesBoth`.
  - `Send_BehaviorOrder_IsRegistrationOrder`.
- Remove the "no reflection on hot path" claim from comments if it is still untrue after the change.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.CQRS.Tests` is green, and the new tests fail on `4097d47f` (except the order regression test).
- [x] No public or protected signature changed.

## Open questions
- none

## Deviations

### Synchronous-throw test split into handler and behavior cases

**Original plan:** `Send_HandlerThrowsSynchronously_ExceptionIsNotWrapped` fails on `4097d47f`.

**Actual implementation:** a handler was never wrapped: the compiled dispatcher calls it directly
inside an async lambda, so its synchronous throw already surfaced unchanged. The wrapping happened
only where `MethodInfo.Invoke` was used — pipeline behaviors and event handlers. The handler test
stays as a regression pin (green on `4097d47f`, like the order test), and
`Send_BehaviorThrowsSynchronously_ExceptionIsNotWrapped` is the test that failed before the fix
(`TargetInvocationException`). The CQRS sources at the start of the step differed from `4097d47f`
only by logging step 05's `OperationsActivitySource`; the new tests were run against them before
the fix: 7 failed, the order and handler-throw pins passed.

### Interface dispatch through generic-method delegates; intent from the `Send` overload

**Original plan:** invoke behaviors through the interface `MethodInfo` via cached compiled
delegates, in `Mediator.cs` and `EventDispatcher.cs`.

**Actual implementation:** behaviors and event handlers are called by a closed generic method that
casts to the interface (`PipelineBehaviorInvoker<TResponse>.Invoke<TRequest>`,
`EventDispatcher.InvokeHandler<TEvent>`), turned into a delegate once per type with
`MethodInfo.CreateDelegate` — no expression tree is needed for a single interface call. The new
internal `Internal/PipelineBehaviorInvoker.cs` holds the behavior cache. Each public `Send` overload
now passes its handler interface (`ICommandHandler<>`, `ICommandHandler<,>`, `IQueryHandler<,>`)
instead of the mediator inferring it from the request's interfaces, which is what lets one type
implementing `IQuery<T>` and `ICommand<T>` reach both handlers. The missing-handler message names
`RegisterQueriesFromAssembly` for queries and `RegisterCommandsFromAssembly` for commands, and says
that internal handlers are not discovered.

### Event-dispatch tests and shared test helpers

**Original plan:** tests in `MediatorTests.cs` only.

**Actual implementation:** the event-handler fix is pinned in `NotificationDispatcherTests.cs`
(`Dispatch_HandlerWithExplicitInterfaceImpl_IsInvoked`,
`Dispatch_HandlerThrowsSynchronously_LogsOriginalException`, both red on the old code). Test types
live in the new `DispatchTestFixtures.cs`, and the new `RecordingLoggerProvider.cs` captures log
entries with their structured properties (reused by step 03).
