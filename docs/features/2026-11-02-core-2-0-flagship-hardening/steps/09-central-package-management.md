---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 09
status: done
---

# Step 09: Central Package Management

## Summary
Package versions were written inline in each `.csproj` (126 `PackageReference` items with a
`Version`), and only the `package-management` skill's table kept them aligned. AvroConvert,
SolTechnology.DateTimeZone and SolTechnology.DataLake already use Central Package Management. This
step moves every version into one root `Directory.Packages.props`, so a version mismatch between
projects is impossible and a bump is one line. Added on 2026-11-03 at the user's request, after the
plan review; it changes no package version and no published dependency.

## Affected components
- `Directory.Packages.props` — NEW — `ManagePackageVersionsCentrally=true`; one `PackageVersion` per package (85 packages, plus the 2 Scheduler references below), grouped as library and testing-kit dependencies, tests, and the DreamTravel sample
- `src/**/*.csproj`, `tests/**/*.csproj`, `tests/Directory.Build.props`, `sample-tale-code-apps/DreamTravel/**/*.csproj` — EDIT — drop the `Version` attribute from every `PackageReference` (37 files)
- `src/SolTechnology.Core.Scheduler/SolTechnology.Core.Scheduler.csproj` — EDIT — explicit `Microsoft.Extensions.Configuration.Binder` and `Microsoft.Extensions.DependencyInjection` (see Deviations)
- `.github/skills/package-management/SKILL.md`, `.github/skills/package-management/references/canonical-versions.md`, `.github/skills/dependency-audit/SKILL.md` — EDIT — versions are added and bumped in `Directory.Packages.props`

## Changes
- No version changed: before the move every package had exactly one version across the repository.
- `Aspire.AppHost.Sdk` keeps its version on the `<Sdk>` element (an MSBuild SDK, not a package reference).

## Acceptance criteria
- [x] `git ls-files '*.csproj' '*.props' | xargs grep -oE '<PackageReference[^>]*Version="'` prints nothing.
- [x] A clean `dotnet build SolTechnology.Core.slnx` (all `bin`/`obj` deleted) is green with no `NU1xxx`.
- [x] The `<dependency>`/`<group>` lines of all 20 packed nuspecs are identical before and after (100 lines).
- [x] `dotnet test SolTechnology.Core.slnx` is green (18 projects, 569 passed, 0 failed).
- [x] Guards, Scheduler and Guards.Tests (outside the solution) still build.

## Open questions
- none

## Deviations

### Scheduler references restored

**Original plan:** none (new step).

**Actual implementation:** building the projects outside the solution showed that `SolTechnology.Core.Scheduler` had stopped compiling at step 07, because it used `Configuration.Binder` and `DependencyInjection` only through the four packages `src/Directory.Build.props` used to add. The project is kept for reference and is not packed, so it gets the two references explicitly (10.0.9, as before); its behaviour is unchanged.
