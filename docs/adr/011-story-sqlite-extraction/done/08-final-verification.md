---
adr: 011-story-sqlite-extraction
step: 08 of 09
status: done
---

# Step 08: Final verification — clean audit + green solution

## Summary
Prove the goal is met end-to-end: the `src/` libraries are free of NU1903, the new sample project +
relocated tests build and pass, and nothing in the running DreamTravel sample regressed.

## Affected components
- none (verification only; no source edits)

## Details
Run, from the repo root, and capture output in the PR:
1. `dotnet restore SolTechnology.Core.slnx --force-evaluate` — must show **zero NU1903** for any `src/`
   library project (specifically `SolTechnology.Core.Story`) and zero NU1903 for
   `SolTechnology.Core.Story.Tests`.
2. `dotnet build SolTechnology.Core.slnx -c Release` — green. `src/` builds under
   `TreatWarningsAsErrors=true` with **no** SQLite `<NuGetAuditSuppress>` anywhere in `src/` or `tests/`.
3. `dotnet test tests/SolTechnology.Core.Story.Tests/SolTechnology.Core.Story.Tests.csproj` — green
   (surviving in-memory + generic registration cases).
4. `dotnet test` on the new `DreamTravel.SQLite.UnitTests` project — green (relocated SQLite round-trip +
   adapted registration cases).
5. Spot-check the DreamTravel sample still uses the in-memory default (no host wired to SQLite):
   `grep -rn "UseSQLiteStoryRepository" sample-tale-code-apps/DreamTravel/src` returns only the
   `DreamTravel.SQLite` extension definition + doc comments — **no call site** in a host composition root.

Cross-check the audit posture: `src/Directory.Build.props` keeps `TreatWarningsAsErrors=true` with only
NU1900/NU1510 demoted, so a surviving NU1903 in `src/` would fail the build — a passing build is the proof.

## Acceptance criteria
- Steps 1–5 all pass as described.
- `grep -rn "GHSA-2m69-gcr7-jv3q" src tests` returns nothing (suppression fully gone from product code).
- The only `GHSA-2m69-gcr7-jv3q` / SQLite-package references in the repo live under
  `sample-tale-code-apps/DreamTravel` (the new project + its test project).
- `SolTechnology.Core.slnx` lists both new projects.

## Open questions
- none

