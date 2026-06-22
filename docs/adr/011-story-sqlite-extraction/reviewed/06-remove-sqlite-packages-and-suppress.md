---
adr: 011-story-sqlite-extraction
step: 06 of 09
status: reviewed
---

# Step 06: Remove the SQLite packages and the CVE suppression from `src/`

## Summary
The CVE-clearing PR. With the SQLite code gone (Step 05), remove the now-unused SQLite
`PackageReference`s, the unused `Newtonsoft.Json`, and the `<NuGetAuditSuppress>` blocks (with their
comments) from both the library and its test project. This is the **plumbing** half of the removal and
the step that actually eliminates CVE-2025-6965 from `src/`.

## Affected components
- `src/SolTechnology.Core.Story/SolTechnology.Core.Story.csproj` — remove `Microsoft.Data.Sqlite.Core`, `SQLitePCLRaw.bundle_green`, `Newtonsoft.Json`, and the `<NuGetAuditSuppress>` block + its explanatory comment; **bump `<Version>` 0.7.0 → 0.8.0** (breaking public-API removal under 0.x — premortem #2)
- `tests/SolTechnology.Core.Story.Tests/SolTechnology.Core.Story.Tests.csproj` — remove the `<NuGetAuditSuppress>` block + its comment

## Details
- From `Story.csproj` `ItemGroup`, delete these three lines:
  - `<PackageReference Include="Microsoft.Data.Sqlite.Core" Version="10.0.9" />`
  - `<PackageReference Include="SQLitePCLRaw.bundle_green" Version="2.1.11" />`
  - `<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />` (verified unused — finding #2)
- Delete the entire `<!-- CVE-2025-6965 ... REMOVE once SQLitePCLRaw ships ... -->` comment **and** the
  `<NuGetAuditSuppress Include="https://github.com/advisories/GHSA-2m69-gcr7-jv3q" />` line in `Story.csproj`.
- **Leave intact** in `Story.csproj`: the `FrameworkReference Microsoft.AspNetCore.App`, the
  `Microsoft.Extensions.*` `Remove` lines, and the AUID/Core `ProjectReference`s — none of those relate
  to SQLite.
- In `Story.Tests.csproj`, delete the `<NuGetAuditSuppress>` line **and** its preceding comment. The
  test project no longer pulls SQLite transitively (the library dropped it), so its own audit pass is
  clean without the suppression.
- This is a `.csproj`-only PR — no `.cs` changes.

## Acceptance criteria
- `dotnet restore SolTechnology.Core.slnx --force-evaluate` produces **zero** NU1903 for
  `SolTechnology.Core.Story` and `SolTechnology.Core.Story.Tests`.
- `dotnet build src/SolTechnology.Core.Story` is green with `TreatWarningsAsErrors=true` and **no**
  suppression present (proves the CVE source is genuinely gone, not masked).
- `grep -rn "GHSA-2m69-gcr7-jv3q" src tests` returns nothing.
- `grep -rn "Sqlite\|SQLitePCLRaw\|Newtonsoft" src/SolTechnology.Core.Story/SolTechnology.Core.Story.csproj`
  returns nothing.

## Open questions
- none

