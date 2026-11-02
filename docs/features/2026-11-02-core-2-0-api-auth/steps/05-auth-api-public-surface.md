---
spec: 2026-11-02-core-2-0-api-auth
step: 05
status: to-do
---

# Step 05: Authentication and Api — public surface cleanup

## Summary
This implements `C-21` and `C-22`. `ApiKeyAuthenticationSchemeOptions.ApiKey` is a public
mutable field, and `ApiKeyAuthenticationHandler` takes a redundant
`ILogger<ApiKeyAuthenticationHandler>` parameter although the base class provides `Logger`.
`ConfigureSwaggerOptions` is a public implementation type of the Api installer.

Former core step 52 (Authentication and Api parts); split per library on 2026-11-02. The Logging part (`C-20`) is logging step 05.

## Affected components
- `src/SolTechnology.Core.Authentication/APIKeyAuthenticationSchemeOptions.cs` — EDIT — `:9` `public string ApiKey { get; set; } = "";`
- `src/SolTechnology.Core.Authentication/APIKeyAuthenticationHandler.cs` — EDIT — primary constructor without the `logger` parameter; use `Logger`
- `src/SolTechnology.Core.API/ModuleInstaller.cs` — EDIT — `:247` `ConfigureSwaggerOptions` → `internal sealed`
- `tests/SolTechnology.Core.Authentication.Tests/**`, `tests/SolTechnology.Core.API.Tests/**` — EDIT

## Changes
- Depends on step 03 (instance logger) having merged.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green, including DreamTravel.
- [ ] `dotnet test` is green for the Authentication and API test projects.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none (`C-21`, `C-22` confirmed by the user on 2026-11-02)

## Deviations
