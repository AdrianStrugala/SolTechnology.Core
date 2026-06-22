---
adr: 011-story-sqlite-extraction
step: 01 of 09
status: reviewed
---

# Step 01: Scaffold the `DreamTravel.SQLite` DataLayer project

## Summary
Create the new (initially code-free) DataLayer project that will host the relocated SQLite Story
provider, wire its SQLite packages + the explicit CVE suppression, reference
`SolTechnology.Core.Story`, and register it in `SolTechnology.Core.slnx`. This is **infrastructure
plumbing only** — deliberately separated from the repository code (Step 02) and the DI extension
(Step 03) so the security-sensitive package/suppress decision gets a crisp, isolated review. The
project compiles empty.

> ✅ **Naming resolved (OQ1).** Per [ADR-001](../../001-acronym-capitalization-refactoring.md) the
> project is **`DreamTravel.SQLite`** (`SQL` acronym uppercase, `ite` lowercase). Folder, csproj,
> assembly, and root namespace all use `DreamTravel.SQLite`.

## Affected components
- `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite/DreamTravel.SQLite.csproj` — new csproj
- `SolTechnology.Core.slnx` — register the new project under `/SampleApps/DreamTravel/src/DataLayer/`

## Details
- Template the csproj on `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.Sql/DreamTravel.Sql.csproj`:
  - **UTF-8 BOM** at the start of the file (the sibling DataLayer csproj files start with a BOM).
  - `ProjectReference` to Story at the verified relative depth:
    `..\..\..\..\..\src\SolTechnology.Core.Story\SolTechnology.Core.Story.csproj` (5 `..` segments —
    matches `DreamTravel.Sql.csproj`). Add `SolTechnology.Core.AUID` only if Step 02 shows the repo
    needs `Auid` directly without the transitive Story reference (Story already references AUID, so a
    transitive reference is expected — confirm at Step 02 and add an explicit reference only if the
    compiler asks).
- `PackageReference`s — **copy the exact versions from `Story.csproj` verbatim at implementation time**
  (currently `Microsoft.Data.Sqlite.Core` **10.0.9** and `SQLitePCLRaw.bundle_green` **2.1.11**). Do
  **not** add `Newtonsoft.Json` — verified unused by the SQLite repo (it uses `System.Text.Json`).
- Add an **explicit, commented** suppression mirroring the one being removed from `Story.csproj`:
  ```
  <!-- CVE-2025-6965 (GHSA-2m69-gcr7-jv3q): SQLite < 3.50.2 memory corruption reached transitively
       via Microsoft.Data.Sqlite.Core -> SQLitePCLRaw.bundle_green -> SQLitePCLRaw.lib.e_sqlite3 2.1.11.
       No patched SQLitePCLRaw release exists (latest 2.1.11). This is a SAMPLE app (warning-level
       audit), so the suppress only keeps build output clean. REMOVE once a patched bundle ships. -->
  <NuGetAuditSuppress Include="https://github.com/advisories/GHSA-2m69-gcr7-jv3q" />
  ```
- Register in `SolTechnology.Core.slnx` inside the existing
  `<Folder Name="/SampleApps/DreamTravel/src/DataLayer/">` block, next to `DreamTravel.Sql.csproj`.
- Set `RootNamespace` to `DreamTravel.SQLite` (matches the project name).

## Acceptance criteria
- `dotnet build` of the new project succeeds (empty project, packages restore).
- `dotnet restore SolTechnology.Core.slnx` resolves the new project; no NU1503/NU1504 (duplicate /
  missing project) errors.
- The csproj begins with a UTF-8 BOM and uses 5-segment `..` depth to reach `src/SolTechnology.Core.Story`.
- No SQLite packages or suppression were added to any `src/` project.

## Open questions
- None. OQ1 resolved: project name is `DreamTravel.SQLite` (ADR-001 acronym casing).

