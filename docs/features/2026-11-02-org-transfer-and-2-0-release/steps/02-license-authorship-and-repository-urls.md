---
spec: 2026-11-02-org-transfer-and-2-0-release
step: 02
status: to-do
---

# Step 02: License, authorship, and repository URLs for the organization

## Summary
Two `Directory.Build.props` changes ship in one premortem-gated PR after the transfer (merged
during plan review, RQ11):

- Authorship: `LICENSE:3` says "Copyright (c) 2026 Adrian Strugala", while the packages say
  `<Copyright>© 2026 Sol Technology</Copyright>` and `<Authors>Adrian Strugala</Authors>`
  (`repo#A14`, `src/Directory.Build.props:25,27`). Q2 (user, 2026-11-02): the holder and author
  is "Adrian Strugala / Sol Technology" everywhere.
- URLs: `AdrianStrugala` appears 46 times in 12 files. The critical occurrences are
  `RepositoryUrl` and `PackageProjectUrl` (`src/Directory.Build.props:28,30`), which drive
  SourceLink and the nuget.org links. Updated URLs appear only on newly published versions, so
  this step must merge before `v2.0.0` is tagged (step 05). Target owner is `sol-technology`
  (Q3, user, 2026-11-02).

## Affected components
- `LICENSE` — EDIT — `:3` → `Copyright (c) 2026 Adrian Strugala / Sol Technology`
- `src/Directory.Build.props` — EDIT — `:25` `<Authors>Adrian Strugala / Sol Technology</Authors>`, `:27` `<Copyright>© 2026 Adrian Strugala / Sol Technology</Copyright>`, `:28,30` repository and project URLs
- `sample-tale-code-apps/DreamTravel/Directory.Build.props` — EDIT — `:5` `<Authors>Adrian Strugala / Sol Technology</Authors>`, `:6` repository URL (premortem trigger: a second `Directory.Build.props`)
- `README.md` — EDIT — `:18` stats badge (`master` raw URL, Q7), `:20` Actions badge, `:39-51`, `:64-70`, `:120-122` doc links
- `docs/CQRS.md` — EDIT — `:164,170,171`
- `docs/Tale.md` — EDIT — `:543,549,557`
- `docs/theAutomation.md` — EDIT — `:13,31,47,53`
- `docs/theQuality.md` — EDIT — `:60` (SHA permalink keeps working via redirect; update the owner), `:63,292`
- `sample-tale-code-apps/DreamTravel/README.md` — EDIT — `:243`
- `.github/skills/README.md` — EDIT — `:78` the rule that requires absolute `AdrianStrugala` URLs now names `sol-technology`
- `.github/workflows/update-nuget-stats.yml` — EDIT — `:20` `owner:` only if the nuget.org owner changed in step 01

## Changes
- MIT text unchanged; only the holder line.
- Exact current URL list: `grep -rn "AdrianStrugala" --include=*.md --include=*.props --include=*.yml --include=*.csproj --include=*.json . | grep -v "docs/features/"`. Historical feature records are not rewritten. Line numbers above may have shifted after the core and showcase features; the grep is authoritative.
- Remaining `AdrianStrugala` mentions are personal author credits only (e.g., `.github/CODEOWNERS` `* @AdrianStrugala`, Q8).

## Acceptance criteria
- [ ] `head -3 LICENSE` and `dotnet msbuild src/SolTechnology.Core.CQRS/SolTechnology.Core.CQRS.csproj -getProperty:Copyright` name the same holder.
- [ ] `dotnet msbuild src/SolTechnology.Core.CQRS/SolTechnology.Core.CQRS.csproj -getProperty:Authors` prints `Adrian Strugala / Sol Technology`.
- [ ] The `AdrianStrugala` grep above lists only intentional author credits.
- [ ] `dotnet pack src/SolTechnology.Core.CQRS/SolTechnology.Core.CQRS.csproj -o /tmp/p` nuspec `<repository url=…>` shows `https://github.com/sol-technology/SolTechnology.Core`.
- [ ] The documentation-cleanup skill reports no broken external links to GitHub.
- [ ] `dotnet build SolTechnology.Core.slnx` is green.
- [ ] Premortem mitigations for both `Directory.Build.props` files are applied.

## Open questions
- none (Q2 resolved by the user on 2026-11-02: "Adrian Strugala / Sol Technology"; Q3 resolved: `sol-technology`)

## Deviations
