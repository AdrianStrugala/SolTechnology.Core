---
spec: 2026-11-02-org-transfer-and-2-0-release
step: 00
status: done
---

# Step 00: Run premortem (gate)

**Agent.** Docs only.

## Summary
A premortem is mandatory (`CLAUDE.md §4`): step 02 edits two `Directory.Build.props` files, and
step 05 publishes the persisted-contract breaks of the core and library features. Run the
[premortem skill](../../../../.github/skills/premortem/SKILL.md) in a session that did not author
this plan, after `review` is `done` and after every prerequisite feature is `completed`. Record the
full output here and the verdict in the `premortem:` field of `summary.md`. This step changes no
`src/` code.

## Affected components
- `docs/features/2026-11-02-org-transfer-and-2-0-release/steps/00-run-premortem.md` — EDIT — the `## Premortem` section below
- `docs/features/2026-11-02-org-transfer-and-2-0-release/summary.md` — EDIT — the `premortem:` verdict
- Step files named by required mitigations, and the feature brief — EDIT

## Changes

- **Modules touched:**
  - GitHub and nuget.org, by manual owner actions (steps 01, 03, 04, 05):
    - the push and CI gate, then the repository transfer;
    - nuget.org organization co-ownership of 27 existing IDs, plus the new `SolTechnology.Core.Auth` created by the `v2.0.0` publish (`C-28`);
    - nuget.org deprecation of the 7 published ghost IDs (step 03) and of `SolTechnology.Core.Authentication` (step 05);
    - security settings, with no `master` ruleset (decision A, R3);
    - a Trusted Publishing policy without an environment;
    - the `NUGET_USER` repository secret.

    No `nuget-release` environment exists (owner, 2026-11-03, R1).
  - Package metadata for every packable `src/SolTechnology.Core.*` project, through `src/Directory.Build.props` (step 02).
  - DreamTravel `Directory.Build.props` (step 02).
  - `LICENSE`, `README.md`, the `docs/*.md` URLs and package-README links, `.github/ISSUE_TEMPLATE/config.yml`, `.github/PULL_REQUEST_TEMPLATE.md`, `.github/skills/README.md`, `docs/PublicDocumentationGuide.md` (step 02).
  - CI: `.github/workflows/publishPackages.yml` (step 04). `update-nuget-stats.yml` and `unlistDeprecatedPackages.yml` are unchanged.
  - Docs: `docs/CICD.md` (steps 03, 04, 05); `CHANGELOG.md`, `docs/migration-2.0.md`, `docs/release-runbook-2.0.md`, and `SolTechnology.Core.slnx` `/Docs/` (step 05).
- **Contract delta:** see the feature brief [`../../2026-11-02-org-transfer-and-2-0-release.md`](../../2026-11-02-org-transfer-and-2-0-release.md):
  - package metadata: `<Authors>`, `<Copyright>`, `RepositoryUrl`, `PackageProjectUrl` (nuspec) — step 02;
  - publishing credential: `NUGET_API_KEY` → Trusted Publishing (OIDC, `id-token: write` on the `publish` job only, no environment) — step 04;
  - repository location: `AdrianStrugala/SolTechnology.Core` → `sol-technology/SolTechnology.Core`, with GitHub redirects — step 01;
  - first public 2.0.0 packages: `C-01`…`C-33` (`C-25` rejected; `C-30`, `C-31` unused), `P-01`…`P-30` (`P-21`, `P-22` unused), `L-01`…`L-07`, and Chronicle's `CH`/`CP`/`LC` rows — step 05. Their own premortems ran in the core, library and Chronicle features; this one covers how they are released and communicated;
  - nuget.org IDs: `SolTechnology.Core.Auth` is new; `SolTechnology.Core.Authentication` stays at 1.x and is deprecated — step 05;
  - semver: MAJOR (2.0.0) published.
- **`ModuleInstaller.cs` edits:** none.
- **`Directory.Build.props` edits:** `src/Directory.Build.props` `:25,27,28,30` and `sample-tale-code-apps/DreamTravel/Directory.Build.props` `:5,6` (step 02).
- **Persisted contracts:** no new change. Step 05 releases the breaks already recorded (`P-03`…`P-07`, `P-17`, `P-18`, `P-27`) with migration guidance.
- **Applicable checklists** (`.github/skills/premortem/references/`): `build-and-nuget.md`, `ci-docs-and-samples.md`.
- **Steps under review:** [01](01-transfer-repository-and-nuget-org.md), [02](02-license-authorship-and-repository-urls.md), [03](03-org-repository-hygiene.md), [04](04-switch-to-trusted-publishing.md), [05](05-release-2-0-0.md); close: [06](06-retrospective.md).

## Acceptance criteria
- [x] Premortem output (frame, imagined failure, scenario table, top 3 risks, required mitigations naming their step files, accepted risks, decision) recorded in this file.
- [x] `summary.md` `premortem:` updated to the verdict in the same change.

## Open questions
- none

## Premortem

### Premortem — Sol Technology org transfer and 2.0.0 release

Run on 2026-11-10 against `master@67ab17c3` (125 commits ahead of `origin/master`) by a session that did not author
the plan. Evidence was read from the repository, nuget.org (flat-container, search and gallery APIs), the GitHub API,
`sol-technology.github.io@c57bf1f`, and the NuGet Trusted Publishing docs (updated 2026-09-01). A local
`dotnet pack -p:RepositoryUrl=…` confirmed that an explicit `RepositoryUrl` wins over the Git remote in the nuspec.

#### Plan resync (before the stories)

| Step | Drift found | Fixed to |
|---|---|---|
| 01 | Verified a `nuget-release` environment that does not exist. The pre-check "CI on `master` is green" could not pass, because 125 commits were unpushed. Counted 28 IDs, but `SolTechnology.Core.BlobStorage.Testing` was never published. Cited `unlistDeprecatedPackages.yml:7-18`. Did not list `chronicle-sql-step-targets` or the showcase owner actions. | Phase A is the CI gate, with the showcase owner actions 1–6 in order: PVR, description and content reports before the push; the push; the green run; the `workflow_dispatch` runs; the first Dependabot run. Then the repository-secret check, 27 IDs listed by name, `:36-39`, and the added brief. |
| 02 | Said 46 occurrences in 12 files, and missed `.github/ISSUE_TEMPLATE/config.yml`, `.github/PULL_REQUEST_TEMPLATE.md`, and the Guards and Scheduler csproj. Used a `/tmp` pack path. Left the stats-bot `owner:` undecided. | 41 occurrences in 15 files from `git grep`; the missing files added; an in-repository pack folder; `owner:` stays `AdrianStrugala`; package-README links made absolute. |
| 03 | Titled "Protect", with a ruleset already dropped; said "8 deprecated IDs". | Re-scoped and renamed to organization hygiene (R3). Adds a deprecation table of 7 IDs, the organization ruleset check, and the AR-6 dismissals. |
| 04 | Policy environment `nuget-release`, `NUGET_USER` as an environment secret, a reviewer's tag check; package glob `SolTechnology.Core.*`. | No environment (NuGet docs: optional, leave empty). `NUGET_USER` is a repository secret. The tag guard and a new version guard; glob `SolTechnology.Core*`; `--skip-duplicate` kept; the "Versioning policy" fix (R5). |
| 05 | Old ranges `P-01…P-17` and `L-01…L-05`; no Chronicle inputs; approval in `nuget-release`; the secret deleted "in `nuget-release`". | `C-01…C-33`, `P-01…P-30`, `L-01…L-07` with the gaps named; Chronicle and chronicle-sql-step-targets inputs; a pre-tag checklist, failure handling and a revocation gate. |
| 06 | `package-release.md` item named the environment and a reviewer. | No environment, the tag and version guards, the unlist key; two new follow-ups. |
| Brief | AR-5 relied on the environment and a reviewer; D2 used the old ranges; counts 28/29. | AR-5 rewritten, AR-6 added, decisions R1–R6, counts 27/28. |

Checked and correct: the 20 packable projects in `SolTechnology.Core.slnx` all evaluate to `2.0.0`. Their IDs are
`SolTechnology.Core`, `.Api`, `.API.Testing`, `.AUID`, `.Auth`, `.Blob`, `.Blob.Testing`, `.Cache`, `.CQRS`,
`.Hangfire`, `.HTTP`, `.HTTP.Testing`, `.Logging`, `.MessageBus`, `.Redis.Testing`, `.ServiceBus.Testing`, `.SQL`,
`.SQL.Testing`, `.Tale` and `.Testing`.
- 19 exist on nuget.org at 1.0.0 (Logging at 1.2.0), and `.Auth` returns 404.
- The unlist workflow's 8 hardcoded ghost IDs share none of the 20, and do not include `Authentication`.
- `dontreadme.md` lists the 7 published ghosts and `Authentication`, with the same replacements as step 03's table.
- The workflow names `Build, test and publish`, `Unlist deprecated packages` and `Update NuGet Statistics`, and the
  secret name `NUGET_API_KEY`, match the files; `NUGET_USER` is new in step 04.
- Every prerequisite brief is `completed`, and `2026-11-04-cache-redis-reconnect` is `abandoned`.

#### Frame

- **Modules touched**: no library code. Package metadata for all 20 packages (`src/Directory.Build.props`), the DreamTravel props, the `publishPackages.yml` publish path, docs, and GitHub and nuget.org settings.
- **API delta**: none in this feature. It publishes the 2.0 deltas recorded in the core brief and adds the package ID `SolTechnology.Core.Auth`.
- **Semver impact**: MAJOR. 2.0.0 is published for the first time; `src/Directory.Build.props:5` already says `2.0.0`.
- **Consumers in workspace**: DreamTravel references the projects, not the packages, so it is unaffected by the publish. External nuget.org consumers of 19 IDs at 1.x, and of `SolTechnology.Core.Authentication`, are the blast radius.

#### Imagined Failure

Two weeks after the transfer, the first `v2.0.0` run fails at the `NuGet/login` step. The policy had been created
from the old repository page and named `AdrianStrugala`, so it never matched the organization's token. After a fix,
a re-run with the glob `SolTechnology.Core.*` pushes nothing, because the base `SolTechnology.Core` package is first
in the push order and outside that glob. The next attempt widens the glob but leaves "new packages" off. Four
packages reach nuget.org before the new `SolTechnology.Core.Auth` ID is rejected, and for an evening consumers can
restore only part of 2.0. Under pressure, someone deletes and re-pushes the tag on a newer commit. When everything is
finally up, the interim key is revoked "because 2.0 is out", and the unlist workflow is left without a credential.
A week later an issue arrives: a consumer upgraded to 2.0 and `SOLCORE001` breaks their warnings-as-errors build.
It names `SolTechnology.Core.Authentication` 1.0.0, a package that has no 2.0 to upgrade to.

#### Failure stories

1. **OIDC subject from the old owner.** The Trusted Publishing policy is created before the transfer, or copied from the old URL, with Repository Owner `AdrianStrugala`. The GitHub token of a run in the organization repository carries `repository_owner: Sol-Technology` and a new owner ID, so nuget.org refuses the exchange and `NuGet/login` fails. The policy also locks to repository and owner IDs on its first publish, so it cannot be "carried over" (NuGet docs, "Policies Pending Full Activation"). Missed assumption: "the redirect covers it". Redirects do not apply to OIDC claims.
2. **Policy expires before the tag.** The policy is created right after the transfer, but steps 02 and 04 take more than 7 days. It is still "temporarily active", lapses to inactive, and the tag run gets 401. Missed assumption: a policy is permanent once created.
3. **Tag guard passes on the wrong commit.** The owner tags `v2.0.0` on the commit pushed in step 01, before steps 02 and 04 (`publishPackages.yml:64-73` compares only the tag and `<Version>`, both `2.0.0`). 2.0.0 is published forever with `AdrianStrugala` URLs and author "Adrian Strugala", through `secrets.NUGET_API_KEY` (`:150`), so Trusted Publishing never runs. The key is then revoked per AR-5, and 2.0.1 cannot publish. Missed assumption: the guard checks "the right release". It checks only the version string.
4. **Partial publish.** `dotnet nuget push ./artifacts/*.nupkg` (`:150`) stops at the first failed file. In name order, `SolTechnology.Core.2.0.0.nupkg` is first and `SolTechnology.Core.Auth.2.0.0.nupkg` is about fifth. A policy without the "new packages" scope publishes Core, Api, API.Testing and AUID, then fails on Auth, leaving 16 packages missing. A policy whose glob is `SolTechnology.Core.*` publishes nothing. A re-run is safe only because `--skip-duplicate` stays on the rewritten push line, and only if the tag is not deleted and re-pushed.
5. **Version mismatch inside the set.** A csproj that overrides `<Version>`, as Logging did in 1.x with `1.2.0`, packs another version while the tag guard (which reads only `src/SolTechnology.Core/SolTechnology.Core.csproj`) passes. The mixed set ships, and nuget.org versions cannot be reused. Today all 20 evaluate to `2.0.0`, so this is a regression guard.
6. **Lockstep `SOLCORE001` on a deprecated ID.** A consumer upgrades every package it can to 2.0.0 and keeps `SolTechnology.Core.Authentication` 1.0.0, or `Story` 0.8.0 or `Flow` 1.0.0. `src/SolTechnology.Core.Lockstep.targets:6-9` takes every `SolTechnology.Core*` reference except Guards and Scheduler, sees two majors, and warns. Under warnings-as-errors the build fails with "Upgrade every SolTechnology.Core.* package together", but no 2.x of that ID exists.
7. **Deprecation or unlisting hits the wrong package.** Deprecations are hand work in the nuget.org UI. Deprecating `…Auth` instead of `…Authentication` (one character apart in a search) marks the new package legacy on release day. Separately, revoking `NUGET_API_KEY` leaves `unlistDeprecatedPackages.yml:49` with no credential, so a later unlist fails with 403. Someone then stores a full-scope key "to make it work", which removes the protection its hardcoded list gives.
8. **The push before the gate rewrites history.** The stats bot commits to `origin/master` every Sunday (`update-nuget-stats.yml:5`). If that lands before the owner pushes the 125 commits, the push is rejected. A reflexive `git pull --rebase` rewrites 125 SHAs, and 71 of them are cited in `docs/features/*.md`. Then the first Linux CI run of 2.0 is also the first run of `runTests.ps1` with Ryuk on (P-28) and of `dreamtravel-tests`. Dependabot's first run opens floor and actions-group PRs, and merging one before the tag moves 2.0.0's floors or `download-artifact`, which runs only on tags.
9. **Old URLs stop redirecting, and package-README links are dead.** 1.x nuspecs and old clones rely on the GitHub redirect. Forking the organization repository back to `AdrianStrugala` recreates `AdrianStrugala/SolTechnology.Core` and silently kills the redirect. Separately, 17 of the 20 package READMEs have repository-relative links (for example `Log.md` → `CQRS.md#chronicle`), which nuget.org does not resolve, so the 2.0.0 package pages ship with 404 links. Step 02's list also missed the issue-template security link and the PR template.
10. **Stats bot, or `NuGet/login`, blocked by the organization.** The organization has an Actions policy that allows only GitHub-made actions, or an organization ruleset that targets all repositories. `NuGet/login` cannot run, or the stats bot's direct push (`update-nuget-stats.yml:65-69`) is rejected. A repository-level `rulesets` check alone would not show the organization rule.
11. **Secret scanning on the showcase repository.** After the transfer, secret scanning lists the DreamTravel local `sa` password and similar values (AR-6). Without a recorded rule, they are either left open (the showcase looks leaky) or "fixed" by scrubbing, against decision B.
12. **Product page.** `sol-technology.github.io/src/config.ts` has no Core entry; its only repository links point at `AdrianStrugala/AvroConvert`. A Core card added before the publish would link 1.x pages and a 404 for `.Auth`.

#### Scenarios

| # | Story (what + mechanism) | Trigger (file:line) | Blast radius | Sev | Lik | Existing control | Mitigation → step |
|---|---|---|---|---|---|---|---|
| 1 | Policy names the old owner, so the OIDC exchange fails | policy setup; `publishPackages.yml:133-150` | all (no 2.0) | H | M | none — gap | RRM-1 → 04 |
| 2 | Policy lapses after its 7-day pending window | policy setup | all (no 2.0) | M | M | none — gap | RRM-2 → 05 |
| 3 | `v2.0.0` tagged on a commit without steps 02 and 04; published with old metadata through the old key | `publishPackages.yml:64-73,150` | public consumers | H | M | tag guard (version only) | RRM-3, RRM-4 → 05 |
| 4 | Partial publish: push stops at Auth (scope) or at the base package (glob) | `publishPackages.yml:150` | public consumers | H | M | `--skip-duplicate` (`:150`), "Re-run failed jobs" in `docs/CICD.md` | RRM-5, RRM-6 → 04; RRM-7 → 05 |
| 5 | A package packs a version other than the tag | `publishPackages.yml:69` reads one csproj | public consumers | H | L | all 20 = 2.0.0 today | RRM-8 → 04 |
| 6 | `SOLCORE001` fires for `Authentication` 1.x or a ghost ID with no 2.x | `src/SolTechnology.Core.Lockstep.targets:6-18` | public consumers | M | H | warning text only | RRM-9 → 05 |
| 7 | Wrong package deprecated; unlist runs without a key, or with an over-scoped one | nuget.org UI; `unlistDeprecatedPackages.yml:36-49` | public consumers | M | L | hardcoded ID list, `UNLIST` confirm | RRM-10 → 03; RRM-11 → 04, 05 |
| 8 | Rejected push turns into a rebase; first Linux CI and Dependabot run land in the release window | `update-nuget-stats.yml:5`; 125 unpushed commits | internal; release | M | M | none — gap | RRM-12, RRM-13 → 01 (and 05) |
| 9 | Redirect broken by a recreated name; relative links 404 on nuget.org; template links missed | `README.md:18,20`; package-README docs; `.github/ISSUE_TEMPLATE/config.yml:4` | public consumers, visitors | M | M | GitHub redirect | RRM-14 → 01; RRM-15 → 02 |
| 10 | Organization Actions policy or organization ruleset blocks `NuGet/login` or the stats push | org settings; `update-nuget-stats.yml:65-69` | release, stats | M | L | job-level `contents: write` | RRM-16 → 01, 03 |
| 11 | Secret scanning alerts on AR-6 values mishandled | DreamTravel `appsettings*.json` | showcase | L | H | AR-6 text | RRM-17 → 03 |
| 12 | Product page links Core too early or with old URLs | `sol-technology.github.io/src/config.ts:33-35` | visitors | L | L | none — gap | RRM-18 → 05, 06 |

Confirmed as already defended:
- Push protection on the owner's push: the 125 commits contain only placeholder secrets (scan on 2026-11-10).
- An ID prefix reservation blocking the new ID: no `SolTechnology.*` prefix is reserved (`verified: false`).
- An explicit `RepositoryUrl` beats the Git remote in the nuspec (local pack, 2026-11-10).
- Secrets survive a transfer (GitHub docs, "What's transferred with a repository").

#### Required mitigations (folded into the steps)

| RRM | Mitigation | Step |
|---|---|---|
| RRM-1 | Create the policy only after the transfer, with these fields: owner `SolTechnology`; repository `sol-technology/SolTechnology.Core`; workflow `publishPackages.yml`; Environment empty (NuGet docs: optional, leave empty without environments); `NUGET_USER` as a repository secret. | 04 |
| RRM-2 | Pre-tag: the policy is active, or within its 7-day window; restart the window if needed. | 05 |
| RRM-3 | Pre-tag checklist on the exact SHA: an ancestor of `origin/master`; CI green; steps 02 and 04 present; `<Version>` = 2.0.0; exact tag command; a run starts. | 05 |
| RRM-4 | Revoke the interim key only when the `v2.0.0` log shows `NuGet/login` succeeded; never fall back to the old key in the workflow. | 05 |
| RRM-5 | Policy scopes: new packages and new versions; glob `SolTechnology.Core*` or none, never `SolTechnology.Core.*`. | 04 |
| RRM-6 | The rewritten push line keeps `-s https://api.nuget.org/v3/index.json --skip-duplicate`; an acceptance grep checks it. | 04 |
| RRM-7 | On a partial publish, fix the cause and use "Re-run failed jobs" on the same run; never delete or re-push the tag. | 05 (runbook 2.0) |
| RRM-8 | A build guard checks the `.nupkg` count against the slnx `src` projects, that every file ends `.<Version>.nupkg`, and that a `.snupkg` exists for each. Forced-failure check on a throwaway branch. | 04 |
| RRM-9 | Migration guide section "Upgrade all packages together", with the deprecated-ID → replacement table; the `Authentication` deprecation message names `SOLCORE001`. | 05 |
| RRM-10 | Deprecation table of the 7 published ghost IDs with alternates; no live ID; `Authentication` only in step 05, after `.Auth` exists; check `…Authentication` vs `…Auth`. | 03, 05 |
| RRM-11 | Optional unlisting runs before the revocation; `docs/CICD.md` documents a short-lived, unlist-only key with a glob over the ghost IDs for later runs. | 04, 05 |
| RRM-12 | Push after `git fetch`; integrate with `git pull --no-rebase`, never rebase; CI green on GitHub's Linux runner (`runTests.ps1` with Ryuk on, `dreamtravel-tests`) before the transfer; re-checked on the tagged SHA. | 01, 05 |
| RRM-13 | Merge no Dependabot PR from the step 01 gate until `v2.0.0` is published, unless deliberately, in which case re-run the gate. | 01, 05 |
| RRM-14 | Never create or fork `AdrianStrugala/SolTechnology.Core`; local remotes are re-pointed. | 01 |
| RRM-15 | `git grep` list of 15 files, including the issue and PR templates; absolute links in every package README; Public Documentation Guide rule 11 widened. | 02 |
| RRM-16 | Organization Actions policy allows `NuGet/login`; `rules/branches/master` returns `[]` (covers organization rulesets); a stats-bot run on `master` after the settings. | 01, 03 |
| RRM-17 | Dismiss AR-6 alerts as "used in tests" against the listed paths; any other alert is a real leak. | 03 |
| RRM-18 | Add the Core card to the product site only after the publish, with organization URLs. | 05, 06 (follow-up) |

#### Top 3 Risks

1. **Scenario 4, partial publish.** It is the one failure that leaves nuget.org in a mixed state that consumers can restore; RRM-5, RRM-6 and RRM-7 together make it recoverable.
2. **Scenario 3, wrong-commit tag.** Irreversible metadata, and it hides that Trusted Publishing never ran before the key is revoked; RRM-3 and RRM-4.
3. **Scenario 1, OIDC subject.** It blocks the whole release, and the "quick fix" path leads straight into scenarios 3 and 4; RRM-1.

#### Accepted Risks

- AR-5 (rewritten): the version comes from the props file, not the tag; mitigated by the tag guard, the version guard and the pre-tag checklist.
- AR-6: the local DreamTravel credentials stay; their alerts are dismissed (RRM-17).
- 1.x packages keep `AdrianStrugala` URLs in their nuspecs forever; they rely on the GitHub redirect (RRM-14).
- Scenario 11 and 12 residuals: low severity, owner-handled.
- The DreamTravel Azure DevOps pipelines (AR-4) may need their GitHub connection re-authorised after the transfer; this is a step 06 follow-up and the files stay.

#### Decision

**Go with mitigations** — the three H scenarios each have a concrete mitigation now written into steps 01–05, the
semver classification (MAJOR) matches `src/Directory.Build.props:5`, and no decision is left to the owner: R1–R6
cover the environment, the reviewer, the ruleset, the gate's placement and the versioning-policy fix.

## Deviations
