---
spec: 2026-11-02-core-2-0-api-auth
step: 08
status: to-do
---

# Step 08: Guide conformance — Api and Authentication

## Summary
Two Api files exceed the size caps: `Api/ModuleInstaller.cs` (264) and
`ApiProblemDetailsFactory.cs` (200) (`http-api#24`). Api types hand-write constructor capture and
use `static class` + `this` (`http-api#23,25`), and so does the Authentication installer. Two file
names in Authentication do not match their type names (Guide §10). This is a behaviour-preserving
refactor; file names are aligned with type names, types are unchanged.

Former core steps 65 (Api and Authentication parts) and 66 (Authentication part); split per library on 2026-11-02. The HTTP.Testing part is testing-kits step 06.

## Affected components
- `src/SolTechnology.Core.API/ModuleInstaller.cs` — EDIT — `BuildInMemorySource` and `ConfigureSwaggerOptions` into their own files (NEW)
- `src/SolTechnology.Core.API/Exceptions/ApiProblemDetailsFactory.cs` — EDIT — below 150 lines (extract title/status resolution into an internal helper file)
- `src/SolTechnology.Core.API/Filters/ExceptionFilter.cs`, `src/SolTechnology.Core.API/Security/SecurityHeadersMiddleware.cs` — EDIT — primary constructors; `:52-61` comment into XML docs
- `src/SolTechnology.Core.API/**/*Extensions.cs` — EDIT — `extension` blocks
- `src/SolTechnology.Core.Authentication/ModuleInstaller.cs` — EDIT — `extension` block
- RENAME files only (types unchanged), each a case-only rename:
  - `src/SolTechnology.Core.Authentication/APIKeyAuthenticationHandler.cs` → `ApiKeyAuthenticationHandler.cs`
  - `src/SolTechnology.Core.Authentication/APIKeyAuthenticationSchemeOptions.cs` → `ApiKeyAuthenticationSchemeOptions.cs`

## Changes
- Case-only renames on macOS/Windows (case-insensitive file systems) need two `git mv` calls: `git mv APIKeyAuthenticationHandler.cs APIKeyAuthenticationHandler.tmp.cs && git mv APIKeyAuthenticationHandler.tmp.cs ApiKeyAuthenticationHandler.cs`. Verify with `git ls-files`, not `ls`.
- No public or protected signature change. ModuleInstaller edits are mechanical (the premortem `di.md` checklist still applies).
- Behaviour-preserving refactor; use the [`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.API src/SolTechnology.Core.Authentication -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `git ls-files src/SolTechnology.Core.Authentication | grep -E "APIKey"` prints nothing.
- [ ] `grep -rn "this IServiceCollection" src/SolTechnology.Core.Authentication` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.API.Tests` and `dotnet test tests/SolTechnology.Core.Authentication.Tests` are green; DreamTravel component tests are green.
- [ ] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations
