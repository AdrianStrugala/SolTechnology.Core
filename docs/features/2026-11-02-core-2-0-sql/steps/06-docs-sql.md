---
spec: 2026-11-02-core-2-0-sql
step: 06
status: to-do
---

# Step 06: Module docs — `docs/SQL.md`

## Summary
`docs/SQL.md` ships as the NuGet README. It uses `AddSQL`, `AddSqlHealthCheck`, a missing
`CreateConnection`, and unsupported appsettings auto-binding, and `UnitOfWork` is undocumented
(`infra#18-19`). Every C# snippet must compile against the 2.0 API.

Former core step 61 (SQL part); split per library on 2026-11-02.

## Affected components
- `docs/SQL.md` — EDIT — `:7-10,15-23,100,150` names; remove binding examples; fix the `--- Testing` heading; document `IUnitOfWork.Begin` (step 02); transient-only retry (step 03); `DeployDacpac` (step 04)
- `docs/ClaudeCodingGuide.md` — EDIT — `:263` `services.AddSQL(sqlConfiguration)` → `services.AddSolSQL(sqlConfiguration)` only (former step 61's `docs/*.md` grep covered it and core 11 re-runs that grep; no other Guide change — Guide self-consistency stays with showcase 04)

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.SQL`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).

## Acceptance criteria
- [ ] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/SQL.md docs/ClaudeCodingGuide.md` prints nothing.
- [ ] Snippets compile in the throwaway project (the PR author states it in the PR description).

## Open questions
- none

## Deviations
