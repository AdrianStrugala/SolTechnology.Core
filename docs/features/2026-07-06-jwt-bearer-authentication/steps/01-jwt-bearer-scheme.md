---
spec: 2026-07-06-jwt-bearer-authentication
step: 01
status: to-do
---

# Step 01: JWT Bearer scheme + installer

## Summary

Adds the JWT Bearer authentication scheme to the package: an options class, an installer
method mirroring the existing `AddSolAuthentication` style, and the package reference. This
is the whole public-API delta of the feature, so it ships as one reviewable PR.

## Affected components

- `src/SolTechnology.Core.Authentication/JwtAuthenticationConfiguration.cs` — NEW — options class
- `src/SolTechnology.Core.Authentication/ModuleInstaller.cs` — EDIT — new installer method
- `src/SolTechnology.Core.Authentication/SolTechnology.Core.Authentication.csproj` — EDIT — package reference + description
- `tests/SolTechnology.Core.Authentication.Tests/ModuleInstallerTests.cs` — EDIT — registration guard tests

## Changes

- NEW `JwtAuthenticationConfiguration`: `string Authority`, `string Audience`,
  `bool RequireHttpsMetadata` (default `true`). XML `<summary>` on the type and members.
- NEW `ModuleInstaller.AddSolJwtAuthentication(this IServiceCollection, JwtAuthenticationConfiguration)`:
  - Throw `ArgumentException` when `Authority` or `Audience` is null/empty — same message
    style as the existing `ApiKey` guard (`ModuleInstaller.cs:14`).
  - `AddOptions<JwtAuthenticationConfiguration>().Configure(...).ValidateOnStart()`
    (`ClaudeCodingGuide §14`).
  - `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` with
    `Authority`, `Audience`, `RequireHttpsMetadata`; `TokenValidationParameters`:
    `ValidateIssuer = true`, `ValidateAudience = true`, `ValidateLifetime = true`.
- EDIT global filter registration: extract a private helper so `AddSolAuthentication` and
  `AddSolJwtAuthentication` never add two `AuthorizeFilter` instances; the
  `AuthorizationPolicyBuilder` lists every registered scheme name
  (`ApiKeyAuthenticationSchemeOptions.AuthenticationScheme`,
  `JwtBearerDefaults.AuthenticationScheme`) so both schemes are challenged when both are
  installed.
- csproj: add `Microsoft.AspNetCore.Authentication.JwtBearer@10.0.9` — confirm exact
  version via the [package-management](../../../../.github/skills/package-management/SKILL.md)
  skill and add the row to `references/canonical-versions.md`.
- csproj: update `<Description>` and `<PackageTags>` — the current text claims "without
  the ceremony of full OAuth/OIDC"; mention JWT Bearer / OIDC resource-server support.
- Tests (NUnit, per [test-writing](../../../../.github/skills/test-writing/SKILL.md)):
  `[TestCase]`-parameterized guard test for missing `Authority` / `Audience`; happy-path
  registration returns the same `IServiceCollection` and resolves the JwtBearer scheme
  from `IAuthenticationSchemeProvider`.

## Acceptance criteria

- [ ] `dotnet build SolTechnology.Core.slnx` green, no new `NU1901`–`NU1904`.
- [ ] `dotnet test tests/SolTechnology.Core.Authentication.Tests` green.
- [ ] `AddSolAuthentication` + `AddSolJwtAuthentication` called together register exactly
      one global `AuthorizeFilter` (asserted by test).
- [ ] Existing consumers compile without change (additive API only).

## Open questions

- none

## Deviations

<!-- Empty at authoring time. Filled by implement-plan when reality diverges from the plan. -->
