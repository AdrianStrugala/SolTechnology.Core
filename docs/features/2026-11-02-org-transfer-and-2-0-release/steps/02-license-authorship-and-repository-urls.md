---
spec: 2026-11-02-org-transfer-and-2-0-release
step: 02
status: to-do
---

# Step 02: License, authorship, and repository URLs for the organization

**Agent** edits the repository. The **owner** pushes the result to `master` (`CLAUDE.md §2`).

## Summary
Two `Directory.Build.props` changes ship in one premortem-gated change after the transfer (step 01),
merged during plan review (RQ11):

- **Authorship.** `LICENSE:3` says "Copyright (c) 2026 Adrian Strugala". The packages say
  `<Copyright>© 2026 Sol Technology</Copyright>` and `<Authors>Adrian Strugala</Authors>`
  (`repo#A14`, `src/Directory.Build.props:25,27`). Q2 (user, 2026-11-02, confirmed 2026-11-10): the
  holder and the author are "Adrian Strugala / Sol Technology" everywhere. `<Company>Sol Technology</Company>`
  (`:26`) stays.
- **URLs.** `git grep -c AdrianStrugala -- ':!docs/features'` counted 41 occurrences in 15 tracked
  files on 2026-11-10. `RepositoryUrl` and `PackageProjectUrl` (`src/Directory.Build.props:28,30`)
  drive the nuspec and the nuget.org links. An explicit `RepositoryUrl` wins over the Git remote in
  the nuspec: a local pack with `-p:RepositoryUrl=…sol-technology…` from an `AdrianStrugala` origin
  wrote the org URL (2026-11-10). Updated URLs appear only in newly published versions, so this step
  must merge before `v2.0.0` is tagged (step 05). The target owner is `sol-technology` (Q3).

## Affected components
- `LICENSE` — EDIT — `:3` → `Copyright (c) 2026 Adrian Strugala / Sol Technology`
- `src/Directory.Build.props` — EDIT — `:25` `<Authors>Adrian Strugala / Sol Technology</Authors>`, `:27` `<Copyright>© 2026 Adrian Strugala / Sol Technology</Copyright>`, `:28,30` repository and project URLs
- `sample-tale-code-apps/DreamTravel/Directory.Build.props` — EDIT — `:5` `<Authors>Adrian Strugala / Sol Technology</Authors>`, `:6` repository URL (premortem trigger: a second `Directory.Build.props`)
- `src/SolTechnology.Core.Guards/SolTechnology.Core.Guards.csproj`, `src/SolTechnology.Core.Scheduler/SolTechnology.Core.Scheduler.csproj` — EDIT — their repository URL (not packable; consistency only)
- `README.md` — EDIT — 25 occurrences: `:18` stats badge (raw `master` URL, Q7), `:20` Actions badge, the package tables `:73-104`, and the doc links
- `docs/CQRS.md` (3), `docs/Tale.md` (2), `docs/theAutomation.md` (4), `docs/theQuality.md` (3; SHA permalinks keep working through the redirect, update the owner anyway) — EDIT
- `sample-tale-code-apps/DreamTravel/README.md` — EDIT — `:29` clone URL
- `.github/ISSUE_TEMPLATE/config.yml` — EDIT — `:4` security policy link, `:7` README link (RRM-15)
- `.github/PULL_REQUEST_TEMPLATE.md` — EDIT — the two `CLAUDE.md` links (RRM-15)
- `.github/skills/README.md` — EDIT — `:78` the rule that requires absolute `AdrianStrugala` URLs now names `sol-technology`
- `docs/PublicDocumentationGuide.md` — EDIT — rule 11 (`:65`) extends from the AI Agent section to every link in a package README (RRM-15)
- Package READMEs (`PackageReadmeSource` docs) — EDIT — relative links become absolute (RRM-15, below)
- `.github/workflows/update-nuget-stats.yml` — unchanged — `:20` `$owner = "AdrianStrugala"` is a nuget.org profile, not the GitHub owner. The personal account stays co-owner of every ID (step 01), and of `.Auth` (step 05).
- `.github/CODEOWNERS` — unchanged — `* @AdrianStrugala` is a personal credit (Q8). The previous owner becomes a collaborator after the transfer, so the rule keeps working.

## Changes
- The MIT text is unchanged; only the holder line changes.
- The current list comes from `git grep -n "AdrianStrugala" -- ':!docs/features'`. Historical feature records are not rewritten. The grep is authoritative; the line numbers above are from 2026-11-10.
- **Package README links (RRM-15).** nuget.org does not resolve repository-relative links. On 2026-11-10, 17 of the 20 `PackageReadmeSource` docs had relative `*.md` links: `Log.md` had 7 (for example `CQRS.md#chronicle`), `CQRS.md` had 6 and `SQL.Testing.md` had 5. These links 404 on the 2.0.0 package pages. Rewrite them in the docs that a csproj packs (`git grep -h -oE "<PackageReadmeSource>[^<]+" -- 'src/*.csproj'`) to `https://github.com/sol-technology/SolTechnology.Core/blob/master/docs/<File>.md#anchor`. In-page `#anchor` links stay relative. No package README has a relative image.
- What remains of `AdrianStrugala` after the edit: `.github/CODEOWNERS`, `update-nuget-stats.yml:20`, and Git history.

## Acceptance criteria
- [ ] `head -3 LICENSE` and `dotnet msbuild src/SolTechnology.Core.CQRS/SolTechnology.Core.CQRS.csproj -getProperty:Copyright` name the same holder.
- [ ] `dotnet msbuild src/SolTechnology.Core.CQRS/SolTechnology.Core.CQRS.csproj -getProperty:Authors` prints `Adrian Strugala / Sol Technology`.
- [ ] `git grep -n "AdrianStrugala" -- ':!docs/features'` lists only `.github/CODEOWNERS` and `update-nuget-stats.yml:20`.
- [ ] `dotnet pack src/SolTechnology.Core.CQRS/SolTechnology.Core.CQRS.csproj -o ./artifacts-check` (a folder inside the repository, deleted afterwards): the nuspec `<repository url=…>` and `<projectUrl>` show `https://github.com/sol-technology/SolTechnology.Core`, and `<authors>` and `<copyright>` show the new holder.
- [ ] For every `PackageReadmeSource` doc, `grep -oE '\]\([^)h#][^)]*\)' docs/<File>.md` prints nothing. Run this once against the parent commit first and check that it does print the 2026-11-10 links.
- [ ] The documentation-cleanup skill reports no broken links to GitHub.
- [ ] `dotnet build SolTechnology.Core.slnx` is green. Guards and Scheduler, which are outside the slnx, build by path.
- [ ] After the owner pushes, CI on `master` is green on GitHub.

## Open questions
- none (Q2 resolved by the user on 2026-11-02, confirmed 2026-11-10: "Adrian Strugala / Sol Technology"; Q3 resolved: `sol-technology`)

## Deviations
