---
spec: 2026-11-02-org-transfer-and-2-0-release
step: 00
status: to-do
---

# Step 00: Run premortem (gate)

## Summary
A premortem is mandatory (`CLAUDE.md §4`): step 02 edits two `Directory.Build.props` files, and
step 05 publishes the core feature's persisted-contract breaks. Run the
[premortem skill](../../../../.github/skills/premortem/SKILL.md) in a session that did not author
this plan, after `review` is `done` and after both prerequisite features are `completed`, so the
evidence is current. Record the full output here and the verdict in `summary.md` `premortem:`.
This step changes no `src/` code.

## Affected components
- `docs/features/2026-11-02-org-transfer-and-2-0-release/steps/00-run-premortem.md` — EDIT — append a `## Premortem` section (before `## Deviations`) with the full skill output
- `docs/features/2026-11-02-org-transfer-and-2-0-release/summary.md` — EDIT — `premortem:` verdict
- Step files named by required mitigations — EDIT (by the executing `implement-plan` session)

## Changes

- **Modules touched:**
  - GitHub and nuget.org (manual owner actions): repository transfer, nuget.org organization co-ownership of 28 IDs, security settings (no `master` ruleset, decision A, user, 2026-11-02), Trusted Publishing policy, `nuget-release` environment secrets (steps 01, 03, 04, 05).
  - Package metadata for every packable `src/SolTechnology.Core.*` project through `src/Directory.Build.props` (step 02).
  - DreamTravel `Directory.Build.props` (step 02).
  - `LICENSE`, `README.md`, `docs/*.md` URLs, `.github/skills/README.md` (step 02).
  - CI: `.github/workflows/update-nuget-stats.yml` (step 02), `.github/workflows/publishPackages.yml` (step 04).
  - Docs: `docs/CICD.md` (steps 03, 04), `CHANGELOG.md`, `docs/migration-2.0.md`, `docs/release-runbook-2.0.md`, `SolTechnology.Core.slnx` `/Docs/` (step 05).
- **Contract delta:** see the feature brief
  [`../../2026-11-02-org-transfer-and-2-0-release.md`](../../2026-11-02-org-transfer-and-2-0-release.md):
  - Package metadata: `<Authors>`, `<Copyright>`, `RepositoryUrl`, `PackageProjectUrl` (nuspec and SourceLink) — step 02.
  - Publishing credential: `NUGET_API_KEY` → Trusted Publishing (OIDC, `id-token: write` on the `publish` job only) — step 04.
  - Repository location: `AdrianStrugala/SolTechnology.Core` → `sol-technology/SolTechnology.Core` with GitHub redirects — step 01.
  - First public 2.0.0 packages: every `C-nn`, P-01…P-17, and L-01…L-05 from the core brief reaches consumers — step 05. Their own premortem was core step 00; this premortem covers how they are released and communicated.
  - Semver: MAJOR (2.0.0) published.
- **`ModuleInstaller.cs` edits:** none.
- **`Directory.Build.props` edits:**
  - `src/Directory.Build.props`: step 02 (`:25` authors, `:27` copyright, `:28,30` repository and project URLs).
  - `sample-tale-code-apps/DreamTravel/Directory.Build.props`: step 02 (`:5` authors, `:6` repository URL).
- **Persisted contracts:** no new change; step 05 releases the persisted-contract breaks recorded in the core brief (P-01…P-07, P-17) with migration guidance.
- **Applicable checklists** (`.github/skills/premortem/references/`): `build-and-nuget.md`.
- **Steps under review:**
  - [01](01-transfer-repository-and-nuget-org.md),
    [02](02-license-authorship-and-repository-urls.md),
    [03](03-protect-org-repository.md),
    [04](04-switch-to-trusted-publishing.md),
    [05](05-release-2-0-0.md).
  - Close: [06](06-retrospective.md).

## Acceptance criteria
- [ ] Premortem output (frame, imagined failure, scenario table, top 3 risks, required mitigations naming their step files, accepted risks, decision) recorded in this file.
- [ ] `summary.md` `premortem:` updated to the verdict in the same change.

## Open questions
- none

## Deviations
