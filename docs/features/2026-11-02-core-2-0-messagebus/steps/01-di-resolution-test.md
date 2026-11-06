---
spec: 2026-11-02-core-2-0-messagebus
step: 01
status: done
---

# Step 01: DI resolution test — MessageBus

## Summary
Existing installer tests only check service descriptors, so they missed resolution failures `B1`, `B2`, and `B4` (`P1-2`). This step adds the MessageBus resolution test. It builds the provider with `ValidateOnBuild = true` and `ValidateScopes = true`, then resolves every public service the module registers. Known-broken registrations land as `[Ignore]`d tests that cite the finding ID, and the later step of this feature that fixes each one removes the `Ignore`. It pins down `B1` (`MessageBusBroker` has an internal constructor), `B2` (`ServiceBusClient` is unregistered), and `infra#13` (`ICorrelationIdService` is not registered).

Former core step 11 (MessageBus part); split per library on 2026-11-02.

## Affected components
- `tests/SolTechnology.Core.MessageBus.Tests/ModuleResolutionTests.cs` — NEW

## Changes
- Pattern (NUnit, FluentAssertions, `// Arrange/Act/Assert`, name `AddSol<Module>_ResolvesAllPublicServices_WithValidation`):
  - `new ServiceCollection().AddLogging()`, then the module's `AddSol*` overloads.
  - `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })`.
  - Resolve every public service type inside `CreateScope()`.
- Dummy connection strings are enough because resolution must not open connections; record a deviation if one does.
- Coverage: three separate tests, because `B1` makes `ValidateOnBuild` throw for every MessageBus registration and would mask the other two:
  - `AddSolMessageBus_ResolvesAllPublicServices_WithValidation` (`IMessagePublisher`, the receiver hosted service). `[Ignore("B1 …")]`.
  - `AddSolServiceBusHealthCheck_RegistrationFactory_Resolves`: builds with `ValidateOnBuild = false`, reads `IOptions<HealthCheckServiceOptions>.Value.Registrations`, and invokes each `Factory(scope.ServiceProvider)`. The health check is a `HealthCheckRegistration` factory (`ServiceBusHealthCheckExtensions.cs:33`), which neither `ValidateOnBuild` nor resolving public services ever invokes. `[Ignore("B2 …")]`.
  - `AddSolMessageBus_WithoutLogging_RegistersCorrelationIdService`: `GetService<ICorrelationIdService>()` is not null without `AddSolLogging`. `[Ignore("infra#13 …")]`.
- Step 02 removes all three ignores.
- Follow the [`test-writing`](../../../../.github/skills/test-writing/SKILL.md) skill. No shared helper in a shipped package.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green. The ignored tests are exactly B1, B2, and infra#13.
- [x] Each ignored test, run alone with its `Ignore` removed locally, fails with the cited reason's exception (`B1`: no usable constructor for `MessageBusBroker`; `B2`: unable to resolve `ServiceBusClient`; `infra#13`: `ICorrelationIdService` is null).

## Open questions
- none

## Deviations

### Resolution test registers one publisher and one receiver

**Original plan:** `AddSolMessageBus_ResolvesAllPublicServices_WithValidation` resolves `IMessagePublisher` and the receiver hosted service after the module's `AddSol*` overloads.

**Actual implementation:** The test also calls `WithQueuePublisher` and `WithQueueReceiver` and resolves the registered `IMessageHandler<T>`, so the scoped handler registration those builders add is validated under `ValidateScopes` too. With its `Ignore` removed the test fails with `AggregateException`: "A suitable constructor for type 'MessageBusBroker' could not be located" (B1); the health-check test fails with "No service for type 'ServiceBusClient' has been registered" (B2); the correlation test fails with "Expected correlationIdService not to be <null>" (infra#13).
