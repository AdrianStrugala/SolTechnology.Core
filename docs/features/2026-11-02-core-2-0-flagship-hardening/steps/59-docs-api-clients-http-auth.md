---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 59
status: to-do
---

# Step 59: Module docs — Api, Clients, HTTP checklist, API/HTTP testing, Auth

## Summary
These docs ship as NuGet READMEs, and their samples call APIs that no longer exist (`P1-1`,
`http-api#11-14`). The Auth rewrite is merged into this step during plan review (RQ11):

- `AddApiCore`, `AddApiCoreFilters`, `UseSwaggerWithVersioning`, `AddVersioning`, and
  `AddHTTPClient` are pre-`AddSol*` names.
- The `docs/Clients.md:116-122` defaults table disagrees with `HttpPolicyConfiguration.cs`
  (10 000 / 2 / 10 000 / 5).
- The `docs/Api.md:76-78` flagship controller injects `IQueryHandler<,>`, which Guide §7 forbids.
- `docs/Auth.md:9-60` describes a removed API (`http-api#11`): `AddAuthenticationAndBuildFilter`,
  an `X-Auth` header with a Base64 scheme, a `Key` property, config-section binding, and
  `UseAuthorization()` before `UseAuthentication()`. The code has `AddSolAuthentication`,
  `X-API-KEY`, a raw key compared in constant time (step 07), and no binding. The same rewrite is
  planned in `2026-07-06-jwt-bearer-authentication` step 04. Q5 (user, 2026-11-02): this feature
  owns the rewrite; the JWT feature stays JWT-only.

Every C# snippet must compile against the 2.0 API.

## Affected components
- `docs/Api.md` — EDIT — `:31-49,199` names; `:76-78` `IMediator.Send(query, HttpContext.RequestAborted)`
- `docs/Clients.md` — EDIT — `:16,49,59,101` names; `:116-122` defaults; `:431` health check name and behaviour after step 45; `IncludeResponseBodyInException` is per-request (design A10); `RequestBuilder` names after step 46
- `docs/HTTP-Production-Checklist.md` — EDIT — `:94,122` names
- `docs/API.Testing.md` — EDIT — fixture disposal (step 36)
- `docs/HTTP.Testing.md` — EDIT — strict mapping default (P-11) and step 53 names
- `docs/Auth.md` — EDIT — rewrite from `src/SolTechnology.Core.Authentication/ModuleInstaller.cs`, `AuthenticationConfiguration.cs`, and `docs/architecture/authentication.md`

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references the six packages, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc). Steps 60 and 61 reuse this method.
- Auth: middleware order `UseAuthentication()` then `UseAuthorization()`; mention the constant-time comparison and the `ApiKey` property (`C-21`, confirmed 2026-11-02); no JWT content (that belongs to the JWT feature).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).

## Acceptance criteria
- [ ] `grep -nE "AddApiCore|AddApiCoreFilters|UseSwaggerWithVersioning|\bAddVersioning\b|AddHTTPClient\b|IQueryHandler<" docs/Api.md docs/Clients.md docs/HTTP-Production-Checklist.md` prints nothing.
- [ ] `grep -nE "AddAuthenticationAndBuildFilter|X-Auth\b" docs/Auth.md` prints nothing.
- [ ] The throwaway snippet project builds green (the PR author states it in the PR description).
- [ ] The documentation-cleanup skill reports no broken links for the 6 files.

## Open questions
- none (Q5 resolved by the user on 2026-11-02: this feature owns the `docs/Auth.md` rewrite)

## Deviations
