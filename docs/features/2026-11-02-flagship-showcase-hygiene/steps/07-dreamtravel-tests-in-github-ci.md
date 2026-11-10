---
spec: 2026-11-02-flagship-showcase-hygiene
step: 07
status: done
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
- [ ] The `dreamtravel-tests` job runs green on a PR. *Owner action: the implementing agent may not push or trigger workflows. The job's script ran green in a Linux container instead (see Verification).*
- [x] `grep -rnE "uses: [^@]+@v[0-9]" .github/workflows` prints nothing.
- [x] The `publish` job declares `needs: [build, dreamtravel-tests]`.
- [x] SRM-17: the `dreamtravel-tests` job has `timeout-minutes` and neither `continue-on-error` nor `if:`; `docs/CICD.md` documents the job and the re-run rule.
- [ ] A forced `Assert.Fail()` in a DreamTravel unit test on a throwaway branch makes the job fail; trigger it with `workflow_dispatch` on that branch or a draft PR (a plain branch push does not trigger the workflow, which listens to `master` pushes, tags, and PRs). Delete the branch afterwards. *Owner action (needs a push and a workflow run); the job script's failure path was rehearsed locally instead (see Verification).*
- [x] `git diff --stat <base> -- sample-tale-code-apps/DreamTravel/devOps` prints nothing, where `<base>` is the commit before this feature's first step (`master` takes direct pushes, so `origin/master` is not a reliable base).

## Open questions
- none

## Deviations

### Job layout

**Original plan:** a new `dreamtravel-tests` job that runs the six named DreamTravel test projects.

**Actual implementation:** as planned, with one choice the plan left open: the job runs `dotnet
test` on each project in a bash loop that keeps going after a failure and ends with one
`::error::` line naming every failed project, so one run shows all Linux-only failures (the same
pattern as `.github/runTests.ps1`). It restores the workloads like the `build` job and lets
`dotnet test` build each project; it does not build the whole solution again.

### SRM outcomes (2026-11-10)

- **SRM-17 — done.** The job has `timeout-minutes: 30`, no `continue-on-error`, and no `if:`;
  `publish` declares `needs: [build, dreamtravel-tests]`. `docs/CICD.md` gains the column in the
  trigger table, a `dreamtravel-tests` section (Linux-only failures are fixed in tests or fixtures,
  never by weakening the job), and the release re-run rule under `publish`. Early warning before
  the first GitHub run: the job's script ran green on Linux (below), so no Linux-only failure is
  known.

### Verification (2026-11-10)

- actionlint 1.7.12 with shellcheck: `publishPackages.yml` and `unlistDeprecatedPackages.yml`
  report nothing. `update-nuget-stats.yml` reports one pre-existing SC2015 info at `:66`
  (untouched; the stats bot stays as it is, Q7).
- Linux rehearsal: a fresh clone of `aeeb1358` inside `mcr.microsoft.com/dotnet/sdk:10.0` (Linux
  arm64, Docker socket mounted, `TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal`) ran
  `dotnet workload restore` and the job's script verbatim: Commands no tests, Queries 7,
  TravelingSalesmanProblem 4, Ui 17, GeolocationDataClients 4 passed / 11 skipped, Component 40
  passed; `All 6 DreamTravel test projects passed.`, exit 0. The GitHub runner is Linux x64, so
  the first real run stays the owner's check.
- Failure path: the same script with a stub `dotnet` that fails for the Queries project printed
  `::error::DreamTravel tests failed in .../DreamTravel.Queries.UnitTests.csproj` and exited 1.
- `git diff --stat b244b728 -- sample-tale-code-apps/DreamTravel/devOps` prints nothing
  (`b244b728` is the commit before step 01).

