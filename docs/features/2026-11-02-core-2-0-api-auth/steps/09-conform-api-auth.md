---
spec: 2026-11-02-core-2-0-api-auth
step: 09
status: done
---

# Step 09: Guide conformance — Api and Auth

## Summary
Two Api files exceed the size caps: `Api/ModuleInstaller.cs` (264) and
`ApiProblemDetailsFactory.cs` (200) (`http-api#24`). Api types hand-write constructor capture and
use `static class` + `this` (`http-api#23,25`), and so does the Auth installer. Two file
names in Auth do not match their type names (Guide §10). This is a behaviour-preserving
refactor; file names are aligned with type names, types are unchanged.

Former core steps 65 (Api and Authentication parts) and 66 (Authentication part); split per library on 2026-11-02. The HTTP.Testing part is testing-kits step 06. Renumbered from 08 on 2026-11-03 (new step 01, `C-28`).

## Affected components
- `src/SolTechnology.Core.API/ModuleInstaller.cs` — EDIT — `BuildInMemorySource` and `ConfigureSwaggerOptions` into their own files (NEW)
- `src/SolTechnology.Core.API/Exceptions/ApiProblemDetailsFactory.cs` — EDIT — below 150 lines (extract title/status resolution into an internal helper file)
- `src/SolTechnology.Core.API/Filters/ExceptionFilter.cs`, `src/SolTechnology.Core.API/Security/SecurityHeadersMiddleware.cs` — EDIT — primary constructors; `:52-61` comment into XML docs
- `src/SolTechnology.Core.API/**/*Extensions.cs` — EDIT — `extension` blocks
- `src/SolTechnology.Core.Auth/ModuleInstaller.cs` — EDIT — `extension` block
- RENAME files only (types unchanged), each a case-only rename:
  - `src/SolTechnology.Core.Auth/APIKeyAuthenticationHandler.cs` → `ApiKeyAuthenticationHandler.cs`
  - `src/SolTechnology.Core.Auth/APIKeyAuthenticationSchemeOptions.cs` → `ApiKeyAuthenticationSchemeOptions.cs`

## Changes
- Case-only renames on macOS/Windows (case-insensitive file systems) need two `git mv` calls: `git mv APIKeyAuthenticationHandler.cs APIKeyAuthenticationHandler.tmp.cs && git mv APIKeyAuthenticationHandler.tmp.cs ApiKeyAuthenticationHandler.cs`. Verify with `git ls-files`, not `ls`.
- No public or protected signature change. ModuleInstaller edits are mechanical (the premortem `di.md` checklist still applies).
- Behaviour-preserving refactor; use the [`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.

## Acceptance criteria
- [x] `find src/SolTechnology.Core.API src/SolTechnology.Core.Auth -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] `git ls-files src/SolTechnology.Core.Auth | grep -E "APIKey"` prints nothing.
- [x] `grep -rn "this IServiceCollection" src/SolTechnology.Core.Auth` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.API.Tests` and `dotnet test tests/SolTechnology.Core.Auth.Tests` are green; DreamTravel component tests are green.
- [x] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations

### What left the Api installer and factory

**Original plan:** move `BuildInMemorySource` and `ConfigureSwaggerOptions` out of `Api/ModuleInstaller.cs`; extract the
title/status resolution of `ApiProblemDetailsFactory.cs` into an internal helper.

**Actual implementation:** `BuildInMemorySource` no longer exists in the Api installer, and moving
`ConfigureSwaggerOptions` alone left 240 lines. The correlation-id stamping of framework `ProblemDetails` (the
`CustomizeProblemDetails` and `InvalidModelStateResponseFactory` lambdas) moved to the internal
`Exceptions/ProblemDetailsCorrelation.cs`, and the long `<code>` examples in the installer's XML docs were cut to short
summaries (`docs/Api.md` carries the examples); commit `b21a9339` says the docs "point to docs/Api.md", but they are
only shortened. The installer has 147 lines. The factory's helper is the internal `Exceptions/ProblemDetailsStatusMap.cs`
(`StatusFor`, `TypeForStatus`, `TitleFor`); `FromError` builds its two `ProblemDetails` shapes in one expression, which
brings the factory to 146 lines.

### Extension blocks also in the Api installer, and a fix-on-touch comment commit

**Original plan:** `extension` blocks for `src/SolTechnology.Core.API/**/*Extensions.cs` and the Auth installer.

**Actual implementation:** `Api/ModuleInstaller.cs` used `static class` + `this` too and was edited in this step, so it
moved into two extension blocks (`IServiceCollection`, `IApplicationBuilder`) with the three `*Extensions.cs` files. The
three- to five-line comments in `ExceptionFilter` and `ResultConversionFilter` were shortened to one line each in a
separate `chore(api)` commit (`CLAUDE.md` §1.3, Guide §9.10 fix on touch); `ExceptionFilter`'s log-level rationale went
to its XML `<remarks>` as planned.

### Public API check and commit split

**Original plan:** compare ref assemblies or use `Microsoft.DotNet.ApiCompat`; one behaviour-preserving refactor.

**Actual implementation:** a throwaway reflection dump (types, constructors, methods with parameter names and defaults,
the extension attribute, properties, fields and constants; compiler-generated extension marker types excluded) was taken
before the step and after the extension-block changes: identical for `SolTechnology.Core.Api.dll` (47 lines) and
`SolTechnology.Core.Auth.dll` (17 lines). The step landed as six commits, one smell each, except that `d8cb1b64`
(`AddSolAuth` extension block) also carries the two case-only renames, because `git mv` had staged them; history was not
rewritten. Each commit ran the full `dotnet test SolTechnology.Core.slnx` (18 projects, 775 passed, 11 skipped, 0
failed), including the DreamTravel component tests.

