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
- `.github/workflows/publishPackages.yml` — EDIT — `actions/checkout`, `actions/setup-dotnet`, `actions/upload-artifact`, `actions/download-artifact` pinned to SHAs with a `# vX.Y.Z` comment
- `.github/workflows/unlistDeprecatedPackages.yml` — EDIT — `:22` SHA pin; `permissions: contents: read`
- `.github/workflows/update-nuget-stats.yml` — EDIT — `:15` SHA pin; `:23,44,47,57` comments in English
- `.github/dependabot.yml` — NEW — `nuget` (directory `/`, weekly, group `Testcontainers*`, `Microsoft.Extensions.*`, `OpenTelemetry*`) and `github-actions` (weekly)

## Changes
- Prerequisite: core steps 02 and 03 are merged ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)); step 03 introduces the `publish` job and the `actions/upload-artifact`/`actions/download-artifact` uses pinned here.
- Dependabot PRs must still pass the `CLAUDE.md §6` rules. The `dependency-audit` skill applies when they raise `NU190x`.

## Acceptance criteria
- [ ] `grep -rnE "uses: [^@]+@v[0-9]" .github/workflows` prints nothing.
- [ ] `grep -rnP "[ąćęłńóśźżĄĆĘŁŃÓŚŹŻ]|Szukaj|Posortuj|Stwórz|Zapisz" .github/workflows` prints nothing.
- [ ] A test run of each workflow (`workflow_dispatch` on a branch) succeeds; publish job skipped.

## Open questions
- none

## Deviations
