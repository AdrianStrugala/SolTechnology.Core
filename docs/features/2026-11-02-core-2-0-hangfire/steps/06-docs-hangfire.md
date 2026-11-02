---
spec: 2026-11-02-core-2-0-hangfire
step: 06
status: to-do
---

# Step 06: Module docs — `docs/Hangfire.md`

## Summary
`docs/Hangfire.md` ships as the NuGet README and uses names that no longer exist:
`AddPersistentEvents`, `AddRecurringJob`, `AddCQRS`, `UseSolTechnologyFilters` (`infra#18-19`). It
contradicts `architecture/background-processing.md` about failed handlers, and invents a
`[SmartRetry]` attribute and a 10-step backoff (`infra#10`). Every C# snippet must compile against
the 2.0 API.

Former core step 61 (Hangfire part); split per library on 2026-11-02.

## Affected components
- `docs/Hangfire.md` — EDIT — `:14,23-38,122-123,162-169,197-201,223-230`; `preventOverlap` works (step 02); `IJob` returns `Result` (step 04, `C-19` confirmed 2026-11-02); job IDs `FullName`, `jobId`, `timeZone`, automatic removal of legacy `Type.Name` IDs (step 04, P-06); 1.x event jobs keep running through the legacy overload until 3.0 (step 03, P-05)

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.Hangfire` and `SolTechnology.Core.CQRS`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).
- Premortem mitigations (core step 00, 2026-11-02), documentation part, all in `docs/Hangfire.md`:
  - RM-6 (scenario 6): deploy Hangfire servers before processes that only enqueue.
  - RM-8 (scenario 8): the per-application tracked-ID scope for services that share one storage, and deleting the 2.x recurring IDs after a rollback to 1.x.
  - RM-9 (scenario 9): the retry semantics delivered in step 04. A thrown exception is not retried. A recoverable `Result` is retried up to the configured count. Or, if step 04 recorded a deviation, state the explicit count.

## Acceptance criteria
- [ ] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/Hangfire.md` prints nothing.
- [ ] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [ ] RM-6, RM-8, RM-9: `docs/Hangfire.md` contains the deploy-order note, the shared-storage and rollback note, and the retry semantics that match step 04's tests.

## Open questions
- none

## Deviations
