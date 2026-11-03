---
spec: 2026-11-02-core-2-0-api-auth
step: 06
status: to-do
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
- [ ] `dotnet build SolTechnology.Core.slnx` is green, including DreamTravel.
- [ ] `dotnet test tests/SolTechnology.Core.Auth.Tests` and `dotnet test tests/SolTechnology.Core.API.Tests` are green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none (`C-21`, `C-22` confirmed by the user on 2026-11-02)

## Deviations
