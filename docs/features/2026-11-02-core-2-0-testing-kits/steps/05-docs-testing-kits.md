---
spec: 2026-11-02-core-2-0-testing-kits
step: 05
status: to-do
---

# Step 05: Module docs — testing kits

## Summary
The testing-kit docs ship as NuGet READMEs and must match the 2.0 kits: `docs/Testing.md:19`
names `Retry.UntilConditionMet` (the method is `Retry.Until`), and the fixture, strict-mapping,
Ryuk, and `C-23` changes from steps 01–03 are undocumented (`P1-1`, `infra#18-19`). Every C#
snippet must compile against the 2.0 API.

Former core steps 59 (API.Testing and HTTP.Testing part), 60 (Redis.Testing part), and 61 (Testing kits part); split per library on 2026-11-02.

## Affected components
- `docs/API.Testing.md` — EDIT — fixture disposal (step 01)
- `docs/HTTP.Testing.md` — EDIT — strict mapping default (P-11) and step 03 names
- `docs/Redis.Testing.md` — EDIT — `Flush` name (`C-23`, confirmed 2026-11-02)
- `docs/Testing.md` — EDIT — `:19` `Retry.Until`; Ryuk note (step 02)
- `docs/SQL.Testing.md`, `docs/Blob.Testing.md`, `docs/ServiceBus.Testing.md` — EDIT — `C-23` names (confirmed 2026-11-02)

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references the seven `*.Testing` packages, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).

## Acceptance criteria
- [ ] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/Testing.md docs/API.Testing.md docs/HTTP.Testing.md docs/Redis.Testing.md docs/SQL.Testing.md docs/Blob.Testing.md docs/ServiceBus.Testing.md` prints nothing.
- [ ] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [ ] The documentation-cleanup skill reports no broken links for `docs/API.Testing.md` and `docs/HTTP.Testing.md`.

## Open questions
- none

## Deviations
