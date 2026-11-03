---
spec: 2026-11-02-core-2-0-api-auth
step: 07
status: to-do
---

# Step 07: Module docs — Api and Auth

## Summary
These docs ship as NuGet READMEs, and their samples call APIs that no longer exist (`P1-1`,
`http-api#11-14`):

- `AddApiCore`, `AddApiCoreFilters`, `UseSwaggerWithVersioning`, and `AddVersioning` are
  pre-`AddSol*` names.
- The `docs/Api.md:76-78` flagship controller injects `IQueryHandler<,>`, which Guide §7 forbids.
- `docs/Auth.md:9-60` describes a removed API (`http-api#11`): `AddAuthenticationAndBuildFilter`,
  an `X-Auth` header with a Base64 scheme, a `Key` property, config-section binding, and
  `UseAuthorization()` before `UseAuthentication()`. The code has `AddSolAuthentication`,
  `X-API-KEY`, a raw key compared in constant time (step 03), and no binding. The same rewrite is
  planned in `2026-07-06-jwt-bearer-authentication` step 04. Q5 (user, 2026-11-02): this plan
  owns the rewrite; the JWT feature stays JWT-only.

Every C# snippet must compile against the 2.0 API.

Former core step 59 (Api and Auth part); split per library on 2026-11-02.

## Affected components
- `docs/Api.md` — EDIT — `:31-49,199` names; `:76-78` `IMediator.Send(query, HttpContext.RequestAborted)`; `:387-389` health-check names (`AddSqlHealthCheck`, `AddServiceBusHealthCheck` → the `AddSol*` names; former step 61 covered them only through its `docs/*.md` grep, which core 11 re-runs)
- `docs/Auth.md` — EDIT — rewrite from `src/SolTechnology.Core.Authentication/ModuleInstaller.cs`, `AuthenticationConfiguration.cs`, and `docs/architecture/authentication.md`

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.API` and `SolTechnology.Core.Authentication`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Auth: middleware order `UseAuthentication()` then `UseAuthorization()`; mention the constant-time comparison and the `ApiKey` property (`C-21`, confirmed 2026-11-02); no JWT content (that belongs to the JWT feature).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).

## Acceptance criteria
- [ ] `grep -nE "AddApiCore|AddApiCoreFilters|UseSwaggerWithVersioning|\bAddVersioning\b|AddHTTPClient\b|IQueryHandler<" docs/Api.md` prints nothing.
- [ ] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/Api.md docs/Auth.md` prints nothing (former step 61 check, scoped to this feature's docs).
- [ ] `grep -nE "AddAuthenticationAndBuildFilter|X-Auth\b" docs/Auth.md` prints nothing.
- [ ] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [ ] The documentation-cleanup skill reports no broken links for the 2 files.

## Open questions
- none (Q5 resolved by the user on 2026-11-02: this plan owns the `docs/Auth.md` rewrite)

## Deviations
