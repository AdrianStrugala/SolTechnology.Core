---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 61
status: to-do
---

# Step 61: Module docs — Bus, Hangfire, SQL, Blob, Testing kits

## Summary
These docs ship as NuGet READMEs and contain names and claims that do not match the code
(`infra#18-19`, `infra#10`):

- `docs/Bus.md` and `docs/Hangfire.md` use names that no longer exist: `AddMessageBus`,
  `WithPublisher`, `WithReceiver`, `AddServiceBusHealthCheck`, `AddPersistentEvents`,
  `AddRecurringJob`, `AddCQRS`, `UseSolTechnologyFilters`.
- `docs/Bus.md` claims topics are created on registration, but `CreateResources` defaults to false.
- `docs/Hangfire.md` contradicts `architecture/background-processing.md` about failed handlers,
  and invents a `[SmartRetry]` attribute and a 10-step backoff.
- `docs/SQL.md` and `docs/Blob.md` use `AddSQL`, `AddSqlHealthCheck`, a missing
  `CreateConnection`, and unsupported appsettings auto-binding. `UnitOfWork` is undocumented.
- `docs/Testing.md:19` names `Retry.UntilConditionMet`; the method is `Retry.Until`.

## Affected components
- `docs/Bus.md` — EDIT — `:7-10,42-43,69-70,94-95,108-111`; `CreateResources`; health check registered by the installer (step 12); dead-letter at `MaxDeliveryCount` (step 34, `C-27`, P-17)
- `docs/Hangfire.md` — EDIT — `:14,23-38,122-123,162-169,197-201,223-230`; `preventOverlap` works (step 13); `IJob` returns `Result` (step 51, `C-19` confirmed 2026-11-02); job IDs `FullName`, `jobId`, `timeZone`, automatic removal of legacy `Type.Name` IDs (step 51, P-06); 1.x event jobs keep running through the legacy overload until 3.0 (step 32, P-05)
- `docs/SQL.md` — EDIT — `:7-10,15-23,100,150` names; remove binding examples; fix the `--- Testing` heading; document `IUnitOfWork.Begin` (step 15); transient-only retry (step 33); `DeployDacpac` (step 49)
- `docs/Blob.md` — EDIT — `:15-23,43`; `using SolTechnology.Core.Blob;`; `CancellationToken` (step 50)
- `docs/Testing.md` — EDIT — `:19` `Retry.Until`; Ryuk note (step 37)
- `docs/SQL.Testing.md`, `docs/Blob.Testing.md`, `docs/ServiceBus.Testing.md` — EDIT — `C-23` names (confirmed 2026-11-02)

## Changes
- Snippet verification as in step 59.
- Premortem mitigations (step 00, 2026-11-02), documentation part, all in `docs/Hangfire.md`:
  - RM-6 (scenario 6): deploy Hangfire servers before processes that only enqueue.
  - RM-8 (scenario 8): the per-application tracked-ID scope for services that share one storage, and deleting the 2.x recurring IDs after a rollback to 1.x.
  - RM-9 (scenario 9): the retry semantics delivered in step 51. A thrown exception is not retried. A recoverable `Result` is retried up to the configured count. Or, if step 51 recorded a deviation, state the explicit count.

## Acceptance criteria
- [ ] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/*.md` prints nothing outside `docs/features/`.
- [ ] Snippets compile in the throwaway project.
- [ ] RM-6, RM-8, RM-9: `docs/Hangfire.md` contains the deploy-order note, the shared-storage and rollback note, and the retry semantics that match step 51's tests.

## Open questions
- none

## Deviations
