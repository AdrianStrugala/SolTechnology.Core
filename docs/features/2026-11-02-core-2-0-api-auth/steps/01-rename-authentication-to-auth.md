---
spec: 2026-11-02-core-2-0-api-auth
step: 01
status: to-do
---

# Step 01: Auth — rename SolTechnology.Core.Authentication to SolTechnology.Core.Auth

## Summary
`C-28` (user, 2026-11-03, `CLAUDE.md §2`): the package `SolTechnology.Core.Authentication`
becomes `SolTechnology.Core.Auth` in 2.0, because authorization joins it later. `AddSolAuth`
is the one entry for authentication and authorization: today it registers the API-key scheme and
the global "require authenticated user" `AuthorizeFilter`, and future roles and policies become
its options. This step only renames. Project, test project, assembly, namespace, NuGet ID,
installer method, configuration type, icon, and every in-repository consumer move together, with
no behaviour change. It runs first, so steps 02–10 edit the new paths. Semver: MAJOR; the
2.0.0 version is already set by core 04.

## Affected components
- Source project. `git mv` the folder, then edit:
  - `src/SolTechnology.Core.Authentication/` → `src/SolTechnology.Core.Auth/` — RENAME (`git mv` of the folder; then delete the untracked `bin/` and `obj/` that moved with it)
  - `src/SolTechnology.Core.Authentication/SolTechnology.Core.Authentication.csproj` → `src/SolTechnology.Core.Auth/SolTechnology.Core.Auth.csproj` — RENAME + EDIT — `<Product>`, `<PackageId>`, `<AssemblyName>` = `SolTechnology.Core.Auth`; `<Title>SolTechnology.Core Auth</Title>`; `<Description>` gains the closing sentence `Formerly SolTechnology.Core.Authentication.`; `<PackageReadmeSource>Auth.md</PackageReadmeSource>` unchanged
  - `src/SolTechnology.Core.Authentication/AuthenticationConfiguration.cs` → `src/SolTechnology.Core.Auth/AuthConfiguration.cs` — RENAME + EDIT — `public class AuthConfiguration`; namespace `SolTechnology.Core.Auth`
  - `src/SolTechnology.Core.Auth/ModuleInstaller.cs` — EDIT — namespace `SolTechnology.Core.Auth`; `AddSolAuthentication(this IServiceCollection services, AuthenticationConfiguration authenticationConfiguration)` → `AddSolAuth(this IServiceCollection services, AuthConfiguration authConfiguration)`; `AddOptions<AuthConfiguration>()`; `nameof(AuthConfiguration)` in the `ArgumentException` message; registrations and their order unchanged
  - `src/SolTechnology.Core.Auth/APIKeyAuthenticationHandler.cs` — EDIT — namespace only (the file-name case fix is step 09; the type stays `ApiKeyAuthenticationHandler`, `C-21` is step 06)
  - `src/SolTechnology.Core.Auth/APIKeyAuthenticationSchemeOptions.cs` — EDIT — namespace only (the type stays `ApiKeyAuthenticationSchemeOptions`)
- Test project:
  - `tests/SolTechnology.Core.Authentication.Tests/` → `tests/SolTechnology.Core.Auth.Tests/` — RENAME (`git mv`; delete moved `bin/`, `obj/`)
  - `tests/SolTechnology.Core.Authentication.Tests/SolTechnology.Core.Authentication.Tests.csproj` → `tests/SolTechnology.Core.Auth.Tests/SolTechnology.Core.Auth.Tests.csproj` — RENAME + EDIT — `ProjectReference` `..\..\src\SolTechnology.Core.Auth\SolTechnology.Core.Auth.csproj`
  - `tests/SolTechnology.Core.Auth.Tests/ModuleInstallerTests.cs` — EDIT — namespace `SolTechnology.Core.Auth.Tests`; `AddSolAuth_WhenApiKeyMissing_ThrowsAtRegistration`, `AddSolAuth_WhenApiKeyProvided_ReturnsSameServiceCollection`; `AuthConfiguration`; `nameof(AuthConfiguration)`
- Solution and CI:
  - `SolTechnology.Core.slnx` — EDIT — `:90` `tests/SolTechnology.Core.Auth.Tests/SolTechnology.Core.Auth.Tests.csproj`; `:109` `src/SolTechnology.Core.Auth/SolTechnology.Core.Auth.csproj`
  - `.github/workflows/publishPackages.yml` — unchanged (verified 2026-11-03): the pack step iterates the slnx projects, the slnx guard iterates `src/*/*.csproj`, and `excluded` names only `SolTechnology.Core.Scheduler` and `SolTechnology.Core.Guards`
  - `.github/workflows/unlistDeprecatedPackages.yml` — unchanged: the old ID is deprecated (release 05), not unlisted
  - `.github/runTests.ps1` — unchanged (reads test projects from the slnx)
  - `src/Directory.Build.props`, `src/SolTechnology.Core.Lockstep.targets` — unchanged: the icon (`docs/icons/$(PackageId).png`) and `buildTransitive/$(PackageId).targets` follow `<PackageId>`
- Icon:
  - `docs/icons/SolTechnology.Core.Authentication.png` → `docs/icons/SolTechnology.Core.Auth.png` — RENAME (`git mv`; added by core 10 in commit `1b21236c`)
- DreamTravel:
  - `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/DreamTravel.Api.csproj` — EDIT — `:35` `ProjectReference` `..\..\..\..\..\src\SolTechnology.Core.Auth\SolTechnology.Core.Auth.csproj`
  - `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Program.cs` — EDIT — `:20` `using SolTechnology.Core.Auth;`; `:126-127` `var authConfiguration = builder.Configuration.GetRequiredSection("Authentication").Get<AuthConfiguration>()!;` and `builder.Services.AddSolAuth(authConfiguration);`
  - DreamTravel configuration section `"Authentication"` (`DreamTravel.Api/appsettings.json:22`, `DreamTravel.Ui/Configuration/ConfigurationExtensions.cs:13,26`, `tests/Component/appsettings.tests.json:2`) — unchanged: a configuration key, not the package name
- Documentation and AI tooling:
  - `README.md` — EDIT — `:43` module row: label `[Auth]`, nuget.org link and badgen badge `SolTechnology.Core.Auth` (the badge reads "not found" until release 05 publishes 2.0.0)
  - `docs/Auth.md` — EDIT — `:3`, `:7` package name `SolTechnology.Core.Auth`; `:28` `AuthConfiguration`; NEW migration note under the title: "Formerly `SolTechnology.Core.Authentication` (1.x). In 2.0 the package, assembly, and namespace are `SolTechnology.Core.Auth`; `AddSolAuthentication` → `AddSolAuth`, `AuthenticationConfiguration` → `AuthConfiguration`." The full rewrite is step 08
  - `dontreadme.md` — EDIT — NEW deprecated-package row: `SolTechnology.Core.Authentication` | `SolTechnology.Core.Auth` | renamed in 2.0 under a new package ID; `AddSolAuthentication` → `AddSolAuth`, `AuthenticationConfiguration` → `AuthConfiguration`, namespace `SolTechnology.Core.Auth`; the row links `docs/Auth.md` like the `BlobStorage` row links `docs/Blob.md`
  - `docs/ClaudeCodingGuide.md` — EDIT — `:358` "Authentication wiring goes through `AddSolAuth(...)` from `SolTechnology.Core.Auth`." (replace whichever name the line has: `AddAuthenticationAndBuildFilter` today, or `AddSolAuthentication` if showcase 04 merged first)
  - `.github/skills/premortem/SKILL.md` — EDIT — `:118` `SolTechnology.Core.Auth`
  - `.github/skills/premortem/references/authentication.md` — EDIT — `:1` `# Auth — \`src/SolTechnology.Core.Auth/\``; file name unchanged
  - `docs/architecture/authentication.md`, `docs/architecture/naming-and-public-api.md` — not edited here; the retrospective (step 11) updates them
  - `nuget-stats.json` — not edited (generated by `update-nuget-stats.yml`)
  - `.github/skills/package-management/references/canonical-versions.md`, `tests/SolTechnology.Core.Tests/BuildHygieneGuardTests.cs` — unchanged (verified 2026-11-03: no mention of the package)

## Changes
- `C-28` exact symbol delta:
  - NuGet ID and assembly `SolTechnology.Core.Authentication` → `SolTechnology.Core.Auth`.
  - Namespace `SolTechnology.Core.Authentication` → `SolTechnology.Core.Auth`.
  - `ModuleInstaller.AddSolAuthentication(IServiceCollection, AuthenticationConfiguration)` → `ModuleInstaller.AddSolAuth(IServiceCollection, AuthConfiguration)`.
  - `AuthenticationConfiguration` → `AuthConfiguration`; `IOptions<AuthenticationConfiguration>` → `IOptions<AuthConfiguration>`.
  - Unchanged names: `ApiKeyAuthenticationHandler`, `ApiKeyAuthenticationSchemeOptions`, `ApiKeyAuthenticationSchemeOptions.AuthenticationScheme`, header `X-API-KEY`.
- No `[Obsolete]` forwarding shim: the old ID stays on nuget.org at 1.x and is deprecated there (release 05).
- `InternalsVisibleTo`: none exists today (`git grep InternalsVisibleTo src/SolTechnology.Core.Authentication src/Directory.Build.props` is empty); a later step that adds one names `SolTechnology.Core.Auth.Tests`.
- Use `git mv` for every rename so history follows the files; check with `git status` that each file shows as `renamed`.
- Out-of-repository follow-up (owner): `scripts/brand-core.py` in `sol-technology.github.io` generates `SolTechnology.Core.Auth.png` instead of `SolTechnology.Core.Authentication.png`.
- `nuget.org` (release 05, manual owner action): deprecate every `SolTechnology.Core.Authentication` 1.x version with the alternate package `SolTechnology.Core.Auth`. The new ID was free on nuget.org on 2026-11-03 (`https://api.nuget.org/v3-flatcontainer/soltechnology.core.auth/index.json` returned 404).
- Release notes and migration: the core brief's `C-28` row and "Release-notes inputs" entry; `docs/migration-2.0.md` gets the `C-28` section in release 05.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [ ] `dotnet test tests/SolTechnology.Core.Auth.Tests` is green with both renamed tests.
- [ ] `pwsh ./.github/runTests.ps1` lists `tests/SolTechnology.Core.Auth.Tests/SolTechnology.Core.Auth.Tests.csproj` and passes.
- [ ] The DreamTravel component tests are green (`git ls-files 'sample-tale-code-apps/DreamTravel/tests/Component/*.csproj' | xargs -n1 dotnet test`).
- [ ] `git ls-files src/SolTechnology.Core.Authentication tests/SolTechnology.Core.Authentication.Tests docs/icons/SolTechnology.Core.Authentication.png` prints nothing, and `git ls-files docs/icons/SolTechnology.Core.Auth.png` prints one line.
- [ ] `git grep -n "SolTechnology.Core.Authentication" -- ':!docs/features' ':!nuget-stats.json'` prints only these intended mentions:
  - `dontreadme.md` — the deprecated-package row (durable).
  - `docs/Auth.md` — the migration note (durable).
  - `src/SolTechnology.Core.Auth/SolTechnology.Core.Auth.csproj` — the `<Description>` sentence "Formerly SolTechnology.Core.Authentication." (durable; nuget.org search).
  - `docs/architecture/authentication.md` — `:3` (temporary; step 11 removes it).
- [ ] `git grep -nE "AddSolAuthentication|AuthenticationConfiguration\b" -- ':!docs/features'` prints only `dontreadme.md`, `docs/Auth.md` (migration note), `docs/architecture/authentication.md:6`, and `docs/architecture/naming-and-public-api.md:13` (the last two until step 11).
- [ ] `dotnet pack SolTechnology.Core.slnx -c Release -o /tmp/auth-pack` produces `/tmp/auth-pack/SolTechnology.Core.Auth.2.0.0.nupkg` and no `SolTechnology.Core.Authentication.*.nupkg`, and `unzip -l` of the Auth package lists `lib/net10.0/SolTechnology.Core.Auth.dll`, `docs/readme.md`, `docs/icon.png`, and `buildTransitive/SolTechnology.Core.Auth.targets`.
- [ ] `unzip -p /tmp/auth-pack/SolTechnology.Core.Auth.2.0.0.nupkg docs/icon.png | cmp - docs/icons/SolTechnology.Core.Auth.png` reports no difference.
- [ ] SOLCORE001 still fires: a throwaway console project (not committed) that references `SolTechnology.Core.Auth` 2.0.0 from `/tmp/auth-pack` (as a local package source, with its 2.0.0 dependencies) and `SolTechnology.Core.Authentication` 1.x from nuget.org prints warning `SOLCORE001` on `dotnet build`; with only `SolTechnology.Core.Auth` 2.0.0 it prints none.
- [ ] `SolTechnology.Core.Auth/ModuleInstaller.cs` registration diff against the old file shows only the renamed method, parameter, and type.
- [ ] The PR uses a breaking Conventional Commit (`refactor(auth)!:` with a `BREAKING CHANGE:` footer naming `C-28`, per the [`commit-message`](../../../../.github/skills/commit-message/SKILL.md) skill).

## Open questions
- none (`C-28` confirmed by the user on 2026-11-03; the user asked the plan to carry `AuthenticationConfiguration` → `AuthConfiguration` inside that rename)

## Deviations
