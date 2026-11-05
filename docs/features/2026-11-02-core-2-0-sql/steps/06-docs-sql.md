---
spec: 2026-11-02-core-2-0-sql
step: 06
status: done
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
- [x] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/SQL.md docs/ClaudeCodingGuide.md` prints nothing.
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).

## Open questions
- none

## Deviations

### One explicit binding snippet stays

**Original plan:** remove the binding examples.

**Actual implementation:** the unsupported `Configuration:Sql` auto-binding and the parameterless `AddSQL()` are gone.
`### Configuration` keeps one `appsettings.json` section and `### Registration` binds it explicitly with
`GetSection("Sql").Get<SQLConfiguration>()` before `AddSolSQL`, as DreamTravel's API host does, because the
[`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md) structure asks for one binding snippet and
Guide §14 binds in `Program.cs`. The page is rewritten in the canonical structure, so its `### Testing` heading keeps the
`SQL.md#testing` anchor that `README.md` and `docs/theQuality.md` link to.

### Snippet verification

**Original plan:** copy every C# block into a throwaway console project that references `SolTechnology.Core.SQL` and
build it; mark snippets that must stay illustrative.

**Actual implementation:** a script copied all 7 C# blocks verbatim into an uncommitted web project under `.scratch/`
that references `SolTechnology.Core.SQL`, `SolTechnology.Core.SQL.Testing`, Dapper 2.1.35, and
`Microsoft.EntityFrameworkCore.SqlServer` 10.0.9, with `TreatWarningsAsErrors`. Statement snippets sit in method bodies
that supply `builder`, `services`, `loggerFactory`, `dacpacPath`, `connectionString`, and `ct`. `dotnet build` reported
0 errors and 0 warnings, so no snippet is marked `// illustrative`.

