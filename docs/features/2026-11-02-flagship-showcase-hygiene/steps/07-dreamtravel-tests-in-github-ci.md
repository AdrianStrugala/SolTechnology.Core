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
- `.github/workflows/publishPackages.yml` — EDIT — NEW job `dreamtravel-tests` (`needs: build`, Docker available on `ubuntu-latest`): `DreamTravel.Commands.UnitTests`, `DreamTravel.Queries.UnitTests`, `DreamTravel.TravelingSalesmanProblem.UnitTests`, `DreamTravel.Ui.UnitTests`, `DreamTravel.GeolocationDataClients.IntegrationTests` (its Google and Michelin tests carry `[Ignore("Paid test")]`; `HttpPolicyConfigurationTests` runs offline), and `DreamTravel.Component.Tests` (SQL Server through `SQLFixture`, WireMock); `DreamTravel.E2E.Tests` (a `HelloWorld` placeholder) stays out; the `publish` job becomes `needs: [build, dreamtravel-tests]`. The job builds again: the `build` job's artifact holds only the packages
- `docs/CICD.md` — EDIT — the trigger table and the job list gain `dreamtravel-tests`; the `publish` section says it waits for `build` and `dreamtravel-tests` (resync 2026-11-10: the page documents every job and was not listed)
- `sample-tale-code-apps/DreamTravel/devOps/` — unchanged (Q9: kept)
- `SolTechnology.Core.slnx` — unchanged (the `/SampleApps/DreamTravel/devOps/` folder entries stay)
- `CLAUDE.md` — unchanged (`§7` "Pipelines" bullet stays accurate because the pipelines stay)

## Changes
- The job fails the workflow on any DreamTravel unit or component test failure.
- Every `uses:` in the new job is pinned to a commit SHA with a `# vX.Y.Z` comment, matching step 02 (its `uses: [^@]+@v[0-9]` check must keep printing nothing).
- A failing DreamTravel test therefore also blocks a tag publish.
- The job runs on push and PR (the workflow's existing triggers). It is not a required status check: `master` has no ruleset (decision A, user, 2026-11-02; release step 03, [`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)).
- Premortem SRM-17 (step 00, 2026-11-10, scenario 17: DreamTravel tests have never run on GitHub's Ubuntu runners; a red or skipped `dreamtravel-tests` also skips the tag publish): the job sets `timeout-minutes`, has no `continue-on-error` and no `if:` (a skipped job that `publish` needs skips `publish` too). A Linux-only failure found on the first PR run is fixed in tests or fixtures, never in production code and never by weakening the job. `docs/CICD.md` tells the release operator to re-run a failed `dreamtravel-tests` job before re-tagging. Early warning: the job's status on every `master` push.
- Prerequisite: core steps 02 and 03 ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)) and step 02 of this feature are merged. Core 02 and 03 are done (verified 2026-11-10): `runTests.ps1` walks only `tests/` projects, so DreamTravel tests still never run in GitHub CI, and `publish` currently `needs: build`.

## Acceptance criteria
- [ ] The `dreamtravel-tests` job runs green on a PR.
- [ ] `grep -rnE "uses: [^@]+@v[0-9]" .github/workflows` prints nothing.
- [ ] The `publish` job declares `needs: [build, dreamtravel-tests]`.
- [ ] SRM-17: the `dreamtravel-tests` job has `timeout-minutes` and neither `continue-on-error` nor `if:`; `docs/CICD.md` documents the job and the re-run rule.
- [ ] A forced `Assert.Fail()` in a DreamTravel unit test on a throwaway branch makes the job fail; trigger it with `workflow_dispatch` on that branch or a draft PR (a plain branch push does not trigger the workflow, which listens to `master` pushes, tags, and PRs). Delete the branch afterwards.
- [ ] `git diff --stat <base> -- sample-tale-code-apps/DreamTravel/devOps` prints nothing, where `<base>` is the commit before this feature's first step (`master` takes direct pushes, so `origin/master` is not a reliable base).

## Open questions
- none

## Deviations
