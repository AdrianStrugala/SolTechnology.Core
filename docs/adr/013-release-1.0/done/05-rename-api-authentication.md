---
adr: 013-release-1.0
step: 05 of 11
status: done
---

<!-- Reviewed (2026-06-30): locked the name map (answers 1/2: AddApiCore→AddSolApiCore,
     AddApiCoreFilters→AddSolApiCoreFilters, UseSecurityHeaders→UseSolSecurityHeaders,
     MapCoreHealthChecks→MapSolHealthChecks); added the repo-wide symbol-string sweep with concrete
     Api hits (M1); switched DreamTravel call sites to symbol greps (M2); noted the Authentication
     signature fix now happens in step 06 (answer 3 = fix). 2026-06-30 (Tale decision): the **Story**
     portion of this step (RegisterStories→AddSolTale, the package/type/namespace rebrand) was
     EXTRACTED into the new step 05b — it grew into the single largest rename in the plan once the
     "Tale noun + package rename" decision landed. This step is now Api + Authentication only. -->

# Step 05: Rename wave 3 — Api + Authentication (rename only, behaviour preserved)

## Summary
Rename the top-level consumer surfaces **`Api`** and **`Authentication`** to the `AddSol*` / `UseSol*`
convention. `Authentication` is renamed **only** here; its `BuildServiceProvider` anti-pattern and
non-chainable return type are fixed in the separate logic step 06, which **does run** (answer 3 =
fix). Api's internal `AddCoreLogging` call was already fixed in step 03, so this wave has no
cross-module breakage. The **Story → Tale rebrand + package rename** that used to live here is now its
own step — [`05b-tale-rebrand-and-package-rename.md`](05b-tale-rebrand-and-package-rename.md) — because
it became the biggest single rename in the plan. Pure plumbing — one PR.

## Affected components
- `src/SolTechnology.Core.Api/ModuleInstaller.cs` — EDIT — `AddApiCore`, `AddApiExceptionHandling`, `AddVersioning`, `UseSwaggerWithVersioning`.
- `src/SolTechnology.Core.Api/ApiCoreMvcOptionsExtensions.cs` — EDIT — `AddApiCoreFilters` → `AddSolApiCoreFilters`.
- `src/SolTechnology.Core.Api/Security/SecurityHeadersApplicationBuilderExtensions.cs` — EDIT — `UseSecurityHeaders` → `UseSolSecurityHeaders`.
- `src/SolTechnology.Core.Api/HealthChecks/HealthChecksEndpointExtensions.cs` — EDIT — `MapCoreHealthChecks` → `MapSolHealthChecks`.
- `src/SolTechnology.Core.Authentication/ModuleInstaller.cs` — EDIT — `AddAuthenticationAndBuildFilter` → `AddSolAuthentication` (signature unchanged here; fixed in step 06).
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Program.cs` — EDIT — call sites by symbol grep (not line number).
- `tests/SolTechnology.Core.Api.Tests/**` — EDIT — call sites.

## Changes
- Rename map (answers 1/2):
  - `AddApiCore` → `AddSolApiCore`; `AddApiExceptionHandling` → `AddSolApiExceptionHandling`;
    `AddVersioning` → `AddSolVersioning`
  - `UseSwaggerWithVersioning` → `UseSolSwaggerWithVersioning`
  - `AddApiCoreFilters` (`MvcOptions`) → `AddSolApiCoreFilters`
  - `UseSecurityHeaders` (`IApplicationBuilder`) → `UseSolSecurityHeaders`
  - `MapCoreHealthChecks` (`IEndpointRouteBuilder`) → `MapSolHealthChecks` (`Core` immediately follows
    `Map`, so it is replaced — never `MapSolCoreHealthChecks`)
  - `AddAuthenticationAndBuildFilter` → `AddSolAuthentication` (still returns `AuthorizeFilter` and
    still calls `BuildServiceProvider()` **here** — both fixed in step 06)
- **`RegisterStories` → `AddSolTale` and the whole Story surface now live in step 05b** — not here.
- **Symbol-string sweep (M1) — authoritative repo-wide for the symbols renamed here.** Known Api hits:
  `Api/ModuleInstaller.cs:26` (`<see cref="ApiCoreMvcOptionsExtensions.AddApiCoreFilters(...)"/>`),
  `:31` (`AddApiCore`), `:34` (`opts.AddApiCoreFilters()`), `:40`
  (`<see cref="AddApiExceptionHandling"/> / <see cref="AddVersioning"/>`);
  `Api/ApiCoreMvcOptionsExtensions.cs:22/27/32` (`AddApiCore`, `AddApiCoreFilters`);
  `Api/Exceptions/*` (exception-handler comments/strings that name the renamed registration). Verify
  with `grep -rn "AddApiCore\|AddApiExceptionHandling\|AddVersioning\|UseSwaggerWithVersioning\|UseSecurityHeaders\|MapCoreHealthChecks\|AddAuthenticationAndBuildFilter" src tests sample-tale-code-apps`
  returning only the new names.
- DreamTravel + test call sites located by **symbol grep**, not line number (`Program.cs` is edited in
  steps 03/04/05).
- No `[Obsolete]` shims.

## Acceptance criteria
- [ ] No public `AddApiCore*` / `AddApiExceptionHandling` / `AddVersioning` / `UseSwaggerWithVersioning` /
      `AddApiCoreFilters` / `UseSecurityHeaders` / `MapCoreHealthChecks` /
      `AddAuthenticationAndBuildFilter` remains in `src/`.
- [ ] `MapSolHealthChecks` (not `MapSolCoreHealthChecks`).
- [ ] The grep above returns only the new names across `src tests sample-tale-code-apps` (XML-doc,
      comments, strings included).
- [ ] `dotnet build SolTechnology.Core.slnx` green; `Api.Tests` pass; DreamTravel compiles and its
      component/E2E tests pass.

## Open questions
- none — all naming decisions resolved at step 00; the Authentication signature fix runs in step 06;
  the Story → Tale rebrand is step 05b.

