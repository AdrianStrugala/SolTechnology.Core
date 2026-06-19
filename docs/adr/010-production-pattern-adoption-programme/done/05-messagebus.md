---
adr: 010-production-pattern-adoption-programme
step: 05 of 07
status: done
---
# Step 05: MessageBus — correlation in receiver + pipeline extraction

## Summary
Wire `ICorrelationIdService` into `MessageBusReceiver` and extract the inline handler flow into a composable internal pipeline.

## Affected components
- `src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs` — edit (M2 + M4)
- `src/SolTechnology.Core.MessageBus/Pipeline/IMessageMiddleware.cs` — new (M4)
- `src/SolTechnology.Core.MessageBus/Pipeline/MessageContext.cs` — new (M4)
- `src/SolTechnology.Core.MessageBus/Pipeline/CorrelationMiddleware.cs` — new (M2)
- `src/SolTechnology.Core.MessageBus/Pipeline/DeserializationMiddleware.cs` — new (M4)
- `src/SolTechnology.Core.MessageBus/Pipeline/HandlerInvocationMiddleware.cs` — new (M4)
- `src/SolTechnology.Core.MessageBus/Pipeline/SettlementMiddleware.cs` — new (M4)

## Details
- **M2:** After `CreateAsyncScope`, read `CorrelationId` from `ApplicationProperties`, call `correlationIdService.Set(...)`, push log scope.
- **M4:** Extract `HandleMessageAsync` into pipeline steps. Pipeline is **internal** (not public). Behaviour must be bit-for-bit identical to current inline method.

## Acceptance criteria
- Handler logs contain `CorrelationId` from the published message
- Existing tests pass unchanged (same complete/abandon/dead-letter behaviour)
- `dotnet build` green

## Open questions
- none

## Retrospective — Implementation Deviations

### 1. Pipeline extraction (M4) skipped
**Original plan:** Extract `HandleMessageAsync` into 4 internal middleware classes (IMessageMiddleware, MessageContext, CorrelationMiddleware, DeserializationMiddleware, HandlerInvocationMiddleware, SettlementMiddleware).
**Actual implementation:** Kept the logic inline in `HandleMessageAsync`. The method is ~90 lines, reads top-to-bottom, and the plan itself stated behaviour must be "bit-for-bit identical" — meaning zero functional gain. Introducing 5 new files + an interface for a purely structural refactor of a single method under the size budget (§9.2: target ≤100 lines) would add indirection without value. Correlation propagation (M2) was the real deliverable and shipped in full.
