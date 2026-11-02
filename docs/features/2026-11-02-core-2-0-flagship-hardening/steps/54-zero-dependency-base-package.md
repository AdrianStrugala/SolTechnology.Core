---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 54
status: to-do
---

# Step 54: Packaging — a truly zero-dependency `SolTechnology.Core`

## Summary
`src/Directory.Build.props:49-54` adds `Microsoft.Extensions.Configuration`,
`.Configuration.Binder`, `.DependencyInjection`, and `.Options` to every package. As a result, the
base `SolTechnology.Core` package, described as "Zero-dependency", ships four dependencies
(`cqrs-tale#18`). Each consuming project should declare what it uses. This step edits
`Directory.Build.props` (premortem gate) and changes consumers' transitive graph (P-13).

## Affected components
- `src/Directory.Build.props` — EDIT — delete the `ItemGroup` at `:49-54`
- `src/SolTechnology.Core.HTTP/SolTechnology.Core.HTTP.csproj` (`:44-47`), `src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj` (`:33-36`), `src/SolTechnology.Core.Tale/SolTechnology.Core.Tale.csproj` (`:30-33`) — EDIT — delete the `<PackageReference Remove="Microsoft.Extensions.*" />` blocks, which become dead once the shared `ItemGroup` is gone (Authentication has no such block)
- `src/SolTechnology.Core.*/SolTechnology.Core.*.csproj` — EDIT — add only the `Microsoft.Extensions.*` references each project needs (`10.0.9`, per [`package-management`](../../../../.github/skills/package-management/SKILL.md))
- `.github/skills/package-management/references/canonical-versions.md` — EDIT — note the per-project ownership

## Changes
- Decide per project by building without the inherited refs and adding back only what fails.
- Projects using the ASP.NET Core shared framework need none (and avoid `NU1510`).

## Acceptance criteria
- [ ] `dotnet pack src/SolTechnology.Core/SolTechnology.Core.csproj -o /tmp/p && unzip -p /tmp/p/SolTechnology.Core.2.0.0.nupkg SolTechnology.Core.nuspec | grep -c "<dependency "` prints `0`.
- [ ] `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU1605|NU190[1-4]"` prints nothing.
- [ ] `pwsh ./.github/runTests.ps1` is green.
- [ ] `grep -rn 'PackageReference Remove="Microsoft.Extensions' src --include=*.csproj` prints nothing.
- [ ] Premortem mitigations for `Directory.Build.props` are applied.

## Open questions
- none

## Deviations
