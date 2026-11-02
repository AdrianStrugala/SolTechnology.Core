---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 11
status: to-do
---

# Step 11: DI resolution tests — infrastructure modules

## Summary
This step adds the same `ValidateOnBuild` + `ValidateScopes` resolution tests as step 10 for
MessageBus, Hangfire, SQL, and Cache. It pins down `B1` (`MessageBusBroker` has an internal
constructor), `B2` (`ServiceBusClient` is unregistered), and `infra#13` (`ICorrelationIdService`
is not registered by MessageBus or Hangfire). Blob is excluded because it has no test project, and
none is created (Q6, user, 2026-11-02; accepted risk AR-1).

## Affected components
- `tests/SolTechnology.Core.MessageBus.Tests/ModuleResolutionTests.cs` — NEW
- `tests/SolTechnology.Core.Hangfire.Tests/ModuleResolutionTests.cs` — NEW
- `tests/SolTechnology.Core.SQL.Tests/ModuleResolutionTests.cs` — NEW
- `tests/SolTechnology.Core.Cache.Tests/ModuleResolutionTests.cs` — NEW
- `tests/SolTechnology.Core.Hangfire.Tests/SolTechnology.Core.Hangfire.Tests.csproj` — EDIT — `PackageReference Hangfire.InMemory 1.0.0` (the version DreamTravel already uses; added to `canonical-versions.md`)
- `.github/skills/package-management/references/canonical-versions.md` — EDIT — NEW `Hangfire.InMemory` `1.0.0` row (test-only)

## Changes
- Same pattern as step 10. Dummy connection strings are enough because resolution must not open connections; record a deviation if one does.
- Coverage and ignores:
  - MessageBus — three separate tests, because `B1` makes `ValidateOnBuild` throw for every MessageBus registration and would mask the other two:
    - `AddSolMessageBus_ResolvesAllPublicServices_WithValidation` (`IMessagePublisher`, the receiver hosted service). `[Ignore("B1 …")]`.
    - `AddSolServiceBusHealthCheck_RegistrationFactory_Resolves`: builds with `ValidateOnBuild = false`, reads `IOptions<HealthCheckServiceOptions>.Value.Registrations`, and invokes each `Factory(scope.ServiceProvider)`. The health check is a `HealthCheckRegistration` factory (`ServiceBusHealthCheckExtensions.cs:33`), which neither `ValidateOnBuild` nor resolving public services ever invokes. `[Ignore("B2 …")]`.
    - `AddSolMessageBus_WithoutLogging_RegistersCorrelationIdService`: `GetService<ICorrelationIdService>()` is not null without `AddSolLogging`. `[Ignore("infra#13 …")]`.
  - Hangfire: `services.AddHangfire(c => c.UseInMemoryStorage())` so `IRecurringJobManager` and `RecurringJobRegistrar` resolve; then `AddSolRecurringJob<TestJob>` plus `AddSolPersistentEvents` without `AddSolLogging`. `[Ignore("infra#13 …")]`.
  - SQL: `ISQLConnectionFactory`, `IUnitOfWork`, the SQL health check.
  - Cache: `ISingletonCache`, `IScopedCache<,>`, local and distributed lock/idempotency registrations.

## Acceptance criteria
- [ ] `dotnet test` on the 4 projects is green. The ignored tests are exactly B1, B2, and infra#13 (MessageBus, Hangfire).
- [ ] Each ignored test, run alone with its `Ignore` removed locally, fails with the cited reason's exception (`B1`: no usable constructor for `MessageBusBroker`; `B2`: unable to resolve `ServiceBusClient`; `infra#13`: `ICorrelationIdService` is null).

## Open questions
- none

## Deviations
