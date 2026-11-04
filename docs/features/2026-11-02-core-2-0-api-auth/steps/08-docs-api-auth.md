---
spec: 2026-11-02-core-2-0-api-auth
step: 08
status: done
---

# Step 08: Module docs — Api and Auth

## Summary
These docs ship as NuGet READMEs, and their samples call APIs that no longer exist (`P1-1`,
`http-api#11-14`):

- `AddApiCore`, `AddApiCoreFilters`, `UseSwaggerWithVersioning`, and `AddVersioning` are
  pre-`AddSol*` names.
- The `docs/Api.md:76-78` flagship controller injects `IQueryHandler<,>`, which Guide §7 forbids.
- `docs/Auth.md:9-60` describes a removed API (`http-api#11`): `AddAuthenticationAndBuildFilter`,
  an `X-Auth` header with a Base64 scheme, a `Key` property, config-section binding, and
  `UseAuthorization()` before `UseAuthentication()`. The code has `AddSolAuth(AuthConfiguration)`
  (renamed from `AddSolAuthentication` in step 01, `C-28`), `X-API-KEY`, a raw key compared in
  constant time (step 04), and no binding. The same rewrite is
  planned in `2026-07-06-jwt-bearer-authentication` step 04. Q5 (user, 2026-11-02): this plan
  owns the rewrite; the JWT feature stays JWT-only.

Every C# snippet must compile against the 2.0 API.

Former core step 59 (Api and Auth part); split per library on 2026-11-02. Renumbered from 07 on 2026-11-03 (new step 01, `C-28`).

## Affected components
- `docs/Api.md` — EDIT — `:31-49,199` names; `:76-78` `IMediator.Send(query, HttpContext.RequestAborted)`; `:387-389` health-check names (`AddSqlHealthCheck`, `AddServiceBusHealthCheck` → the `AddSol*` names; former step 61 covered them only through its `docs/*.md` grep, which core 11 re-runs)
- `docs/Auth.md` — EDIT — rewrite from `src/SolTechnology.Core.Auth/ModuleInstaller.cs`, `src/SolTechnology.Core.Auth/AuthConfiguration.cs`, and `docs/architecture/authentication.md`; keep the step 01 migration note (formerly `SolTechnology.Core.Authentication`) verbatim

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.API` and `SolTechnology.Core.Auth`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Auth: middleware order `UseAuthentication()` then `UseAuthorization()`; mention the constant-time comparison and the `ApiKey` property (`C-21`, confirmed 2026-11-02); no JWT content (that belongs to the JWT feature).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).

## Acceptance criteria
- [x] `grep -nE "AddApiCore|AddApiCoreFilters|UseSwaggerWithVersioning|\bAddVersioning\b|AddHTTPClient\b|IQueryHandler<" docs/Api.md` prints nothing.
- [x] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/Api.md docs/Auth.md` prints nothing (former step 61 check, scoped to this feature's docs).
- [x] `grep -nE "AddAuthenticationAndBuildFilter|X-Auth\b" docs/Auth.md` prints nothing.
- [x] `grep -n "SolTechnology.Core.Authentication" docs/Auth.md` prints only the step 01 migration note line.
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [x] The documentation-cleanup skill reports no broken links for the 2 files.

## Open questions
- none (Q5 resolved by the user on 2026-11-02: this plan owns the `docs/Auth.md` rewrite)

## Deviations

### Snippet compile check recorded here, not in a PR description

**Original plan:** the PR author states the snippet result in the PR description.

**Actual implementation:** there is no PR (commits go straight to `master`), so the result is recorded here. All 22 C#
blocks (17 in `docs/Api.md`, 5 in `docs/Auth.md`) were copied verbatim into a throwaway web project under the
git-excluded `.scratch/snippets` that referenced `SolTechnology.Core.API`, `.Auth`, `.CQRS`, `.API.Testing`, `.SQL`,
`.SQL.Testing`, `.Cache`, `.MessageBus` and `.HTTP`; statement blocks were wrapped in methods, and consumer types
(`Trip`, `GetTripQuery`, `ITripRepository`, the two exception types, `PaymentsHealthCheck`, `INotificationPublisher`)
were stubbed. `dotnet build` reported 0 errors; the project was deleted. No snippet is marked `// illustrative`. The
`documentation-cleanup` link check found one relative link (`theQuality.md`), which exists.

### Api.md corrections beyond the planned names

**Original plan:** rename the pre-`AddSol*` names, switch the controller to `IMediator.Send(query,
HttpContext.RequestAborted)`, and fix the health-check names.

**Actual implementation:** compiling every block surfaced more 1.x names, now fixed: `UseSecurityHeaders` →
`UseSolSecurityHeaders`, `MapCoreHealthChecks` → `MapSolHealthChecks`, `AddRedisHealthCheck` → `AddSolRedisHealthCheck`,
and `AddUpstreamHttpHealthCheck<MyReport>(name, url)` → `AddSolUpstreamHttpHealthCheck<PaymentsHealthCheck>(name)`. The
`Error` example is a handler method without the `IQueryHandler<,>` interface, so the page carries no `IQueryHandler<`
text; `Result<T>` is attributed to `SolTechnology.Core`; the behaviour matrix gained the 201/202 row (step 05) and
"status of the `Error` subtype" instead of a non-existent `error.StatusCode`; the exception table gained
`HttpRequestException` → 502/504 and a title table for P-09 (step 03); "What ships in DI" says the filters are scoped
and resolved from DI. The `APIFixture` service-override example uses neutral consumer types (`INotificationPublisher`)
instead of `IHangfireNotificationPublisher`, which no package ships.

### Auth.md follows the canonical page structure

**Original plan:** rewrite from the installer, the configuration type and `docs/architecture/authentication.md`.

**Actual implementation:** the page now uses the `PublicDocumentationGuide.md` sections (Features, Registration,
Configuration, Usage, Testing, Conventions, What ships in DI) and keeps the step 01 migration note verbatim under the
lead. The Testing example adds the `X-API-KEY` header to `CreateAnonymousClient()` and states that
`CreateAuthorizedClient` sets the `Authorization` header, which this scheme does not read.

