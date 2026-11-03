---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 05
status: done
---

# Step 05: DreamTravel — Development-only developer exception page

## Summary
The showcase app enables `UseDeveloperExceptionPage()` in every environment (`S6`,
`sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Program.cs:174`). This step
limits it to `Development`. Scope change (decision B, user, 2026-11-02): the committed DreamTravel
credentials (`repo#B6`) target local Docker/localhost only, so they stay in the repository; this
step no longer scrubs them (accepted risk AR-6 in the brief). The `"dupa"` CORS constant sits in
the edited `Program.cs`, so it is fixed on touch in a separate `chore:` commit (Guide §15).

Former core step 08; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Program.cs` — EDIT — `:174` gated by `app.Environment.IsDevelopment()`; `:33` CORS constant
- `sample-tale-code-apps/DreamTravel/tests/Component/` — EDIT — NEW test `UnhandledException_InProduction_ReturnsProblemDetailsWithoutDeveloperPage` (host environment `Production`, a throwing endpoint or fake upstream; asserts no `text/html` developer page and no stack trace)
- DreamTravel `appsettings*.json`, `docker-compose.yml`, database `Dockerfile`, `DreamTravel.Aspire`, `devOps/` — unchanged (decision B: local-only dev credentials stay)

## Changes
- `if (app.Environment.IsDevelopment()) { app.UseDeveloperExceptionPage(); }`.
- `chore:` commit: `private static readonly string CorsPolicy = "dupa"` → `const string DreamTravelCorsPolicy = "DreamTravelCors"`. Also remove `avroconvertonline.azurewebsites.net` from the allowed origins at `Program.cs:78` (`repo#B12`).
- No credential change (decision B, user, 2026-11-02; AR-6).

## Acceptance criteria
- [x] `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [x] `dotnet test sample-tale-code-apps/DreamTravel/tests/Component/DreamTravel.Component.Tests.csproj` is green (Docker).
- [x] `Program.cs` has no `"dupa"`.
- [x] `S6` regression: the new Production-environment component test fails on `4097d47f` and passes after the change.
- [x] `git diff --stat origin/master -- sample-tale-code-apps/DreamTravel` lists only `Program.cs` and files under `tests/Component/`.

## Open questions
- none

## Deviations

### Generic error handler outside Development

**Original plan:** gate `UseDeveloperExceptionPage()` with `app.Environment.IsDevelopment()`.

**Actual implementation:** the `else` branch also calls `app.UseExceptionHandler()`, so Production answers an unhandled exception with a generic `application/problem+json` 500 through the `IProblemDetailsService` that `AddSolApiCore` registers; without it TestServer and Kestrel return an empty 500 and the planned test name ("ReturnsProblemDetails") could not hold. This matches the `ExceptionFilter` contract ("UseExceptionHandler in Production decides"). The test throws from an `IStartupFilter` branch appended after the app pipeline and boots the host with `environment=Production`; before the fix it failed because the response carried the exception message and full stack trace with local file paths. All 19 DreamTravel component tests pass.
