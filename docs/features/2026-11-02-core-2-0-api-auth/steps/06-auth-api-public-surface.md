---
spec: 2026-11-02-core-2-0-api-auth
step: 06
status: done
---

# Step 06: Auth and Api — public surface cleanup

## Summary
This implements `C-21` and `C-22`. `ApiKeyAuthenticationSchemeOptions.ApiKey` is a public
mutable field, and `ApiKeyAuthenticationHandler` takes a redundant
`ILogger<ApiKeyAuthenticationHandler>` parameter although the base class provides `Logger`.
`ConfigureSwaggerOptions` is a public implementation type of the Api installer.

Former core step 52 (Authentication and Api parts); split per library on 2026-11-02. The Logging part (`C-20`) is logging step 05. Renumbered from 05 on 2026-11-03 (new step 01, `C-28`).

## Affected components
- `src/SolTechnology.Core.Auth/APIKeyAuthenticationSchemeOptions.cs` — EDIT — `:9` `public string ApiKey { get; set; } = "";`
- `src/SolTechnology.Core.Auth/APIKeyAuthenticationHandler.cs` — EDIT — primary constructor without the `logger` parameter; use `Logger`
- `src/SolTechnology.Core.API/ModuleInstaller.cs` — EDIT — `:247` `ConfigureSwaggerOptions` → `internal sealed`
- `tests/SolTechnology.Core.Auth.Tests/**`, `tests/SolTechnology.Core.API.Tests/**` — EDIT

## Changes
- Depends on step 04 (instance logger) having merged.

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green, including DreamTravel.
- [x] `dotnet test tests/SolTechnology.Core.Auth.Tests` and `dotnet test tests/SolTechnology.Core.API.Tests` are green.
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none (`C-21`, `C-22` confirmed by the user on 2026-11-02)

## Deviations

### Two commits instead of one

**Original plan:** one PR for the Auth and Api parts.

**Actual implementation:** `C-21` and `C-22` were committed separately (`refactor(auth)!` and `refactor(api)!`) so each
breaking commit names one package. The handler became a primary constructor; the handler tests construct it without the
logger and still see its Warnings through the `ILoggerFactory` the base class uses. `ModuleInstaller.cs` changed only in
the visibility of `ConfigureSwaggerOptions`; the registration lambda is untouched, so the premortem `di.md` checklist is
unaffected. Neither type is referenced outside its package (DreamTravel included).

