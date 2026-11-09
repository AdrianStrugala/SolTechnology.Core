---
spec: 2026-11-02-core-2-0-messagebus
step: 06
status: done
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
- [x] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/Bus.md` prints nothing.
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).

## Open questions
- none

## Deviations

### Health check is added by the consumer, not registered by the installer

**Original plan:** Describe the health check as "registered by the installer (step 02)".

**Actual implementation:** Step 02 made `AddSolMessageBus` register the keyed `ServiceBusClient` and the options the
check needs; the check itself is still added with `AddSolServiceBusHealthCheck()` on `AddHealthChecks()`. `docs/Bus.md`
says that.

### "Running several instances" section (coordinator, 2026-11-09)

**Original plan:** Not part of the step.

**Actual implementation:** Added under `### Usage` as requested by the coordinator: queue and shared-subscription
receivers are competing consumers, fan-out needs a per-instance subscription (not provided), delivery is at least once
(idempotent handlers, link to the Cache idempotency store), no ordering across instances, and `CreateResources` tolerates
concurrent creation (commit `57c060e5`).

Verification: the page was rewritten to the `PublicDocumentationGuide.md` structure. All six C# blocks compile
unchanged in a throwaway project under `.scratch/messagebus/docsnippets` (registration and health check as top-level
statements, the message, publisher and handler types, and the NUnit test with its handler) that references
`SolTechnology.Core.MessageBus` and `SolTechnology.Core.ServiceBus.Testing`; no snippet is marked `// illustrative`.
The settlement table matches the receiver tests of step 03. `dotnet pack` ships the page as `docs/readme.md`.

