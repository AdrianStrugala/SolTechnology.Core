---
spec: 2026-11-02-org-transfer-and-2-0-release
step: 03
status: to-do
---

# Step 03: Protect the org repository — security settings and presentation (no `master` ruleset)

## Summary
After the transfer, the organization enables the security settings the plan relies on (D1, `repo`
org checklist). This step covers the repository side; the publishing credential moves to Trusted
Publishing in step 04 (split from the former single protection step during plan review, RQ11).
Scope change (user, 2026-11-02, decision A): `master` gets no ruleset or branch protection. CI
keeps running the test suite on push and PR, direct pushes to `master` stay allowed, and the
weekly `update-nuget-stats.yml` commit keeps pushing to `master` as today, so no bypass actor and
no workflow edit are needed. The step keeps secret scanning, push protection, Private
Vulnerability Reporting (Q8), a read-only default `GITHUB_TOKEN`, repository presentation, and the
nuget.org deprecation of the 8 deprecated IDs.

## Affected components
- GitHub org/repo settings — MANUAL — secret scanning, push protection, Private Vulnerability Reporting (Q8), Actions default `GITHUB_TOKEN` = read
- GitHub org/repo settings — MANUAL — no ruleset and no branch protection rule on `master` (decision A, user, 2026-11-02)
- `.github/workflows/update-nuget-stats.yml` — unchanged — the `update-stats` job already declares `permissions: contents: write`, so the read-only default token does not stop its push
- GitHub repo settings — MANUAL — topics (`dotnet`, `cqrs`, `workflow`, `nuget`, `tale-code`), description, homepage = product site, social preview `docs/logo.png`
- nuget.org — MANUAL — mark the 8 deprecated IDs as deprecated, pointing to their replacements
- `docs/CICD.md` — EDIT — repository security settings; `master` has no ruleset (CI runs on push and PR, direct pushes allowed, `build` and `dreamtravel-tests` are not required checks)

## Changes
- Organization: `sol-technology` (Q3).
- No ruleset, no bypass actor, no GitHub App, no new repository secret (decision A supersedes RQ7 (a)).

## Acceptance criteria
- [ ] `gh api repos/sol-technology/SolTechnology.Core --jq '.security_and_analysis'` shows secret scanning and push protection `enabled`.
- [ ] Private Vulnerability Reporting is enabled (owner-confirmed, dated).
- [ ] `gh api repos/sol-technology/SolTechnology.Core/actions/permissions/workflow --jq '.default_workflow_permissions'` prints `read`.
- [ ] `gh api repos/sol-technology/SolTechnology.Core/rulesets` returns `[]` (decision A).
- [ ] A `workflow_dispatch` run of `update-nuget-stats.yml` after the settings are applied pushes its commit to `master`.

## Open questions
- none (Q3, Q8 resolved by the user on 2026-11-02; RQ7 superseded by decision A, user, 2026-11-02)

## Deviations
