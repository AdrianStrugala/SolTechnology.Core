---
name: package-management
description: Look up canonical NuGet package versions used in this repo before adding a PackageReference. Use when adding a new package to any .csproj, creating a new project, or checking which version of Microsoft.Extensions, Polly, FluentValidation, NUnit / xUnit, FluentAssertions, NSubstitute, AutoFixture, Azure SDK, or other shared packages to use. Avoids drift and version-by-memory hallucination.
---
# Package Management
Single source of truth for which version of which package this repo uses. Prevents the failure
mode *"I added `<PackageReference Include="..." Version="..."/>` with a number pulled from memory"*.
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
3. **If the package is not listed**, search existing `.csproj` files for a version already in
   use: `grep -rE 'Include="<Package>" Version="' src/ tests/`. Prefer the **highest** version
   found.
4. **If still not found anywhere**, pick a version aligned with the family already in use
   (e.g. for a new `Microsoft.Extensions.*` package, match the `10.0.x` line).
5. **After adding the package**, update `references/canonical-versions.md` if the package is new
   or the version is newer than what was listed. The skill is only useful if the table stays
   current.
## Key rules
- MUST use the version listed in `canonical-versions.md` when present.
- When versions differ across projects, PREFER the highest — file a follow-up to align the
  laggards in [ADR-006 implementation tracking](../../../docs/adr/006-implementation-plan-workflow.md)
  or open an issue.
- NEVER pin a package below the version in `canonical-versions.md` without a CVE / breaking-change
  justification documented in the `.csproj` near the reference.
- Source: [`nuget.org`](https://www.nuget.org/). No internal feed is configured for this repo.
- New external dependencies in `src/SolTechnology.Core.*` are a `CLAUDE.md §1` forbidden action
  — get user confirmation, check [`nuget-stats.json`](../../../nuget-stats.json), then add.
## Constraints
- DO NOT add a version to `canonical-versions.md` without first verifying it is what `dotnet add`
  resolved.
- DO NOT use this skill as a substitute for checking CVEs — that is `CLAUDE.md §5`.
- DO NOT replace `Newtonsoft.Json` with `System.Text.Json` in MessageBus or Hangfire-integrated
  code without an ADR — both serialisers have documented uses (see canonical table).
- DO NOT invent a freehand version table when this skill is unavailable. STOP and tell the user
  `package-management` is required (CLAUDE.md §2). Drift across projects is the failure mode
  this skill exists to prevent.
