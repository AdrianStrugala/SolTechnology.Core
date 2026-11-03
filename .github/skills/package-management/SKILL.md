---
name: package-management
description: Look up canonical NuGet package versions used in this repo before adding a PackageReference. Use when adding a new package to any .csproj, creating a new project, or checking which version of Microsoft.Extensions, Polly, FluentValidation, NUnit, FluentAssertions, NSubstitute, AutoFixture, Azure SDK, or other shared packages to use. Avoids drift and version-by-memory hallucination.
---
# Package Management
Single source of truth for which version of which package this repo uses. Prevents the failure
mode *"I added a package version with a number pulled from memory"*.

The repo uses **Central Package Management**: every version lives once, as a `PackageVersion` item in
the root [`Directory.Packages.props`](../../../Directory.Packages.props). Project files carry only
`<PackageReference Include="..." />`; a `Version` attribute there fails restore with `NU1008`.
`tests/Directory.Build.props` still adds the shared test packages to every test project, without
versions.
## When to use
- Adding a new `PackageReference` to any `.csproj`.
- Creating a new project that pulls in core dependencies.
- Checking which version is current for an upgrade or CVE fix.
- Resolving a `NU1605` / package-downgrade warning across projects.
## Procedure
1. **Read [`references/canonical-versions.md`](references/canonical-versions.md)** — find the
   package row.
2. **Use the exact version listed.** Do not pick a different version unless a documented reason
   exists (e.g. CVE fix only available higher; new package only available newer).
3. **If the package is not listed**, check `Directory.Packages.props`: an existing
   `PackageVersion` item is the version to use.
4. **If still not found anywhere**, pick a version aligned with the family already in use
   (e.g. for a new `Microsoft.Extensions.*` package, match the `10.0.x` line).
5. **Add the package:** a `<PackageVersion Include="<Package>" Version="<x.y.z>" />` item in the
   matching `ItemGroup` of `Directory.Packages.props` (only if it is not there yet), and a
   versionless `<PackageReference Include="<Package>" />` in the project.
6. **After adding the package**, update `references/canonical-versions.md` if the package is new
   or the version is newer than what was listed. The skill is only useful if the table stays
   current.
## Key rules
- MUST use the version listed in `canonical-versions.md` when present.
- One version per package for the whole repo. Do not use `VersionOverride` without a documented
  CVE or breaking-change reason next to it.
- NEVER pin a package below the version in `canonical-versions.md` without a CVE / breaking-change
  justification documented in `Directory.Packages.props` next to the version.
- Source: [`nuget.org`](https://www.nuget.org/). No internal feed is configured for this repo.
- New external dependencies in `src/SolTechnology.Core.*` are a `CLAUDE.md §2` forbidden action
  — get user confirmation, check [`nuget-stats.json`](../../../nuget-stats.json), then add.
## Constraints
- DO NOT add a version to `canonical-versions.md` without first verifying it is what `dotnet add`
  resolved.
- DO NOT use this skill as a substitute for checking CVEs — that is `CLAUDE.md §6`.
- DO NOT replace `Newtonsoft.Json` with `System.Text.Json` in MessageBus or Hangfire-integrated
  code without a dated feature plan and current architecture update — both serializers have
  documented uses (see canonical table).
- DO NOT invent a freehand version table when this skill is unavailable. STOP and tell the user
  `package-management` is required (CLAUDE.md §3). Drift across projects is the failure mode
  this skill exists to prevent.
