---
spec: 2026-11-02-core-2-0-cqrs
step: 02
status: to-do
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
- [ ] `dotnet test tests/SolTechnology.Core.CQRS.Tests` is green, and the new tests fail on `4097d47f` (except the order regression test).
- [ ] No public or protected signature changed.

## Open questions
- none

## Deviations
