---
spec: 2026-11-02-flagship-showcase-hygiene
step: 07
status: to-do
---

# Step 07: DreamTravel — tests in GitHub CI

## Summary
The slnx build in GitHub CI includes DreamTravel, but its tests never run there. They run only in
the Azure DevOps pipelines (`repo#B11`). A separate GitHub Actions job runs them so a showcase
regression fails CI on push and PR (not a required check; decision A, user, 2026-11-02). Per Q9 (user, 2026-11-02) the Azure DevOps pipelines
(`devOps/pipelines/`), `devOps/scripts/`, and `devOps/infrastructure/legacy_AzureResourceGroup/`
are kept as a memento and are not edited or deleted by this step.

## Affected components
- `.github/workflows/publishPackages.yml` — EDIT — NEW job `dreamtravel-tests` (`needs: build`, Docker available on `ubuntu-latest`): unit tests plus `DreamTravel.Component.Tests`; Google integration tests stay skipped; the `publish` job becomes `needs: [build, dreamtravel-tests]`
- `sample-tale-code-apps/DreamTravel/devOps/` — unchanged (Q9: kept)
- `SolTechnology.Core.slnx` — unchanged (the `/SampleApps/DreamTravel/devOps/` folder entries stay)
- `CLAUDE.md` — unchanged (`§7` "Pipelines" bullet stays accurate because the pipelines stay)

## Changes
- The job fails the workflow on any DreamTravel unit or component test failure.
- Every `uses:` in the new job is pinned to a commit SHA with a `# vX.Y.Z` comment, matching step 02 (its `uses: [^@]+@v[0-9]` check must keep printing nothing).
- A failing DreamTravel test therefore also blocks a tag publish.
- The job runs on push and PR (the workflow's existing triggers). It is not a required status check: `master` has no ruleset (decision A, user, 2026-11-02; release step 03, [`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)).
- Prerequisite: core steps 02 and 03 ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)) and step 02 of this feature are merged.

## Acceptance criteria
- [ ] The `dreamtravel-tests` job runs green on a PR.
- [ ] `grep -rnE "uses: [^@]+@v[0-9]" .github/workflows` prints nothing.
- [ ] The `publish` job declares `needs: [build, dreamtravel-tests]`.
- [ ] A forced `Assert.Fail()` in a DreamTravel unit test on a throwaway branch makes the job fail. Revert it before committing.
- [ ] `git diff --stat origin/master -- sample-tale-code-apps/DreamTravel/devOps` prints nothing.

## Open questions
- none

## Deviations
