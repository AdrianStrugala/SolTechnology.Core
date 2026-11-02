---
spec: 2026-11-02-core-2-0-blob
step: 04
status: to-do
---

# Step 04: Module docs — `docs/Blob.md`

## Summary
`docs/Blob.md` ships as the NuGet README. It uses names that no longer exist and unsupported
appsettings auto-binding (`infra#18-19`), and it predates the 2.0 namespace and cancellation
(step 02). Every C# snippet must compile against the 2.0 API.

Former core step 61 (Blob part); split per library on 2026-11-02.

## Affected components
- `docs/Blob.md` — EDIT — `:15-23,43`; `using SolTechnology.Core.Blob;`; `CancellationToken` (step 02)

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.Blob`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).

## Acceptance criteria
- [ ] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/Blob.md` prints nothing.
- [ ] Snippets compile in the throwaway project (the PR author states it in the PR description).

## Open questions
- none

## Deviations
