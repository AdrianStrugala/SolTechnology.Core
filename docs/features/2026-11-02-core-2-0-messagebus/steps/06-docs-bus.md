---
spec: 2026-11-02-core-2-0-messagebus
step: 06
status: to-do
---

# Step 06: Module docs — `docs/Bus.md`

## Summary
`docs/Bus.md` ships as the NuGet README and uses names that no longer exist: `AddMessageBus`,
`WithPublisher`, `WithReceiver`, `AddServiceBusHealthCheck` (`infra#18-19`). It also claims topics
are created on registration, but `CreateResources` defaults to false. Every C# snippet must
compile against the 2.0 API.

Former core step 61 (Bus part); split per library on 2026-11-02.

## Affected components
- `docs/Bus.md` — EDIT — `:7-10,42-43,69-70,94-95,108-111`; `CreateResources`; health check registered by the installer (step 02); dead-letter at `MaxDeliveryCount` (step 03, `C-27`, P-17)

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.MessageBus`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).

## Acceptance criteria
- [ ] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/Bus.md` prints nothing.
- [ ] Snippets compile in the throwaway project (the PR author states it in the PR description).

## Open questions
- none

## Deviations
