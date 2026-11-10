---
spec: 2026-11-02-flagship-showcase-hygiene
step: 02
status: to-do
---

# Step 02: Workflow supply-chain hardening

## Summary
Every action is pinned to a moving tag (`@v4`), and the unlist workflow has no explicit
least-privilege block (`repo#A8`). Nothing keeps NuGet packages or actions up to date
(`repo#A6`). The stats workflow has Polish comments (`repo#A15`, `P1-5`). This step pins actions
to commit SHAs, adds Dependabot for `nuget` and `github-actions`, and translates the comments.

## Affected components
- `.github/workflows/publishPackages.yml` — EDIT — `:20,22,87,102,107` (`actions/checkout`, `actions/setup-dotnet` twice, `actions/upload-artifact`, `actions/download-artifact`) pinned to SHAs with a `# vX.Y.Z` comment
- `.github/workflows/unlistDeprecatedPackages.yml` — EDIT — `:23` SHA pin (`actions/setup-dotnet`, its only `uses:`); `permissions: contents: read` (still missing at `c0eb9482`)
- `.github/workflows/update-nuget-stats.yml` — EDIT — `:15` SHA pin; `:23,44,47,57,60` comments in English (`:60` "Pokaż podsumowanie" was missing from the original list)
- `docs/CICD.md` — EDIT — the pipeline page documents every workflow and job; add the SHA-pin rule and Dependabot (resync 2026-11-10: not listed before, and step 08 does not cover this page)
- `.github/dependabot.yml` — NEW — `nuget` (directory `/`, weekly, group `Testcontainers*`, `Microsoft.Extensions.*`, `OpenTelemetry*`) and `github-actions` (weekly)

## Changes
- Prerequisite: core steps 02 and 03 are merged ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)); step 03 introduces the `publish` job and the `actions/upload-artifact`/`actions/download-artifact` uses pinned here. Done (verified 2026-11-10). Core 03 shipped without the planned `nuget-release` environment (its Deviations): `publish` runs for `refs/tags/v*` only, and `NUGET_API_KEY` stays a repository secret shared with the unlist workflow.
- Dependabot PRs must still pass the `CLAUDE.md §6` rules. The `dependency-audit` skill applies when they raise `NU190x`.

## Acceptance criteria
- [ ] `grep -rnE "uses: [^@]+@v[0-9]" .github/workflows` prints nothing.
- [ ] `grep -rnP "[ąćęłńóśźżĄĆĘŁŃÓŚŹŻ]|Szukaj|Posortuj|Stwórz|Zapisz" .github/workflows` prints nothing.
- [ ] A test run of each workflow (`workflow_dispatch` on a branch) succeeds; publish job skipped.

## Open questions
- none

## Deviations
