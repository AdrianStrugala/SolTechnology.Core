---
spec: 2026-11-02-org-transfer-and-2-0-release
step: 05
---

# Step 05: Release 2.0.0

**Mixed.** An **agent** writes the release notes, the migration guide, the runbook and the doc links.
The **owner** pushes, tags `v2.0.0`, checks nuget.org, deprecates `SolTechnology.Core.Authentication`,
and revokes the interim key.

## Summary
This step publishes the 20 `SolTechnology.Core.*` packages at 2.0.0 from tag `v2.0.0` in the
organization repository, with release notes and a migration guide. The release carries every 2.0
row of the core brief:
- the confirmed `C-01`…`C-33`;
- the behavioural and persisted-contract breaks `P-01`…`P-30`;
- the log contract changes `L-01`…`L-07`.

The core brief's "Release-notes inputs" (2026-11-10) records the gaps: `C-30`, `C-31`, `P-21` and
`P-22` were reserved by the abandoned
[`2026-11-04-cache-redis-reconnect`](../../2026-11-04-cache-redis-reconnect.md) and never shipped, and
`C-25` was rejected. None of them goes into the notes. Chronicle, which is new in 2.0, has its own
tables. The 1.0 runbook is the template. This is the first run of Trusted Publishing (step 04).
Publishing is tag-only, and the tag guard replaces a reviewer: there is no environment (owner,
2026-11-03). The interim `NUGET_API_KEY` is revoked only after the publish succeeds through the
login step (AR-5).

## Affected components
- `CHANGELOG.md` — EDIT (agent) — replace the `## [2.0.0] - Unreleased` placeholder with `## [2.0.0] - YYYY-MM-DD`:
  - Security (`S3`–`S6`) and Fixed (`B1`–`B11`, P1);
  - Breaking (every confirmed `C-nn` that changes or removes a symbol) and Additive (`C-26`, `C-27`, `C-29`, `C-32`, `C-33`);
  - Behaviour (`P-01`…`P-30` without `P-21`, `P-22`) and Logging (`L-01`…`L-07`);
  - Chronicle (new in 2.0);
  - keep the floor-change lines already under 2.0.0.
- `docs/migration-2.0.md` — NEW (agent) — per package:
  - before/after code for each confirmed `C-nn` that changes a signature, including `C-28` (Authentication → Auth);
  - persisted-data actions for:
    - `P-03` (restart 1.x tales);
    - `P-04` (the `Version` column and a conditional save);
    - `P-05` (1.x event jobs keep running through the legacy overload until 3.0; no drain needed);
    - `P-06` (recurring IDs move to the full name, legacy `Type.Name` entries are removed at start, `RecurringJobsOptions.RemoveStaleRecurringJobs` is opt-in);
    - `P-07` (cold cache; set `InstanceName`);
    - `P-17` (set `MaxDeliveryCount` to the entity's value);
    - `P-18` (a nullable `LeaseExpiresAt` column);
    - `P-27` (a custom `ICorrelationIdService` adds `Clear()`);
  - start-time validation (`P-19`, `P-20`, `P-23`, `P-24`);
  - custom Tale repositories use `TaleJsonOptions.Default` (`C-26`);
  - the rollback and deploy-order notes from the Hangfire inputs;
  - a section "Upgrade all packages together", for RRM-9:
    - `SOLCORE001` fires when the resolved `SolTechnology.Core.*` packages span more than one major (`src/SolTechnology.Core.Lockstep.targets:4-18`);
    - it also fires for a deprecated ID left in the graph: `SolTechnology.Core.Authentication` 1.x, and `ApiClient`, `Story`, `BlobStorage`, `Faker` or `Flow`, none of which has a 2.x;
    - include a table that maps each deprecated ID to its replacement, so a consumer who sees the warning for `Authentication` knows to switch to `SolTechnology.Core.Auth` and not wait for an `Authentication` 2.0.
- `docs/release-runbook-2.0.md` — NEW (agent) — adapt `docs/release-runbook-1.0.md` to:
  - tag-only publishing with the tag guard and the version guard;
  - no environment and no reviewer;
  - Trusted Publishing;
  - the pre-tag checklist and the failure handling below.
- `docs/CICD.md` — EDIT (agent) — point both runbook links to `release-runbook-2.0.md`.
- `README.md` — EDIT (agent) — link `docs/migration-2.0.md`.
- `SolTechnology.Core.slnx` — EDIT (agent) — add both new docs to `/Docs/`.
- Git tag `v2.0.0` — MANUAL (owner) — see "Pre-tag checklist".
- nuget.org — MANUAL (owner), after the publish succeeds:
  - `SolTechnology.Core.Auth`: confirm that `SolTechnology` is an owner. Add `AdrianStrugala` as co-owner, so that `update-nuget-stats.yml`'s `owner:AdrianStrugala` query and the README totals include it.
  - `SolTechnology.Core.Authentication`: deprecate every version, with:
    - reason "Legacy";
    - alternate package `SolTechnology.Core.Auth` (any version);
    - message: "Renamed to SolTechnology.Core.Auth in 2.0. SOLCORE001 names this package until you switch. See https://github.com/sol-technology/SolTechnology.Core/blob/master/docs/migration-2.0.md."

    Deprecate only; do not unlist. Check that the page you deprecate is `…Authentication`, not `…Auth`.
- Optional unlisting — MANUAL (owner), **before** the revocation (RRM-11): if the ghost IDs should be hidden from search, run `Unlist deprecated packages` with `confirm = UNLIST` while the interim key still exists. The job's 8 hardcoded IDs (`unlistDeprecatedPackages.yml:36-39`) overlap none of the 20 live IDs, nor `Authentication`.
- GitHub repository secret and nuget.org — MANUAL (owner), last: delete the `NUGET_API_KEY` repository secret and revoke the key on nuget.org.
- Product site (separate repository) — follow-up, not in this repository: `sol-technology.github.io` has no Core entry yet. `src/config.ts` lists only AvroConvert. Its Core entry links `https://github.com/sol-technology/SolTechnology.Core` and nuget.org 2.0.0, and is added only after this step (RRM-18).

## Changes
- **Release-notes sources.** The [`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md) brief provides:
  - the tables "Public API changes requiring `CLAUDE.md §2` confirmation" (`C-01`…`C-33`), "Behavioural and persisted-contract breaks" (`P-01`…`P-30`) and "Log contract changes" (`L-01`…`L-07`);
  - its "Release-notes inputs" section, including the gap list and the list of rows amended outside the brief. Where an amendment in a library brief's `Deviations` or code-review section differs from the row, the amendment wins.

  The library briefs are listed under "Per-library regroup". Chronicle contributes:
  - the `CH-nn`, `CP-nn` and `LC-nn` tables and the "Release-notes inputs" section of [`2026-11-02-logging-chronicle`](../../2026-11-02-logging-chronicle.md): the `ITimingService` removal, the retired EventId 2147, and EventIds 2154/2155;
  - [`2026-11-10-chronicle-sql-step-targets`](../../2026-11-10-chronicle-sql-step-targets.md): SQL steps are named `SQL {OPERATION} {target}`. This is a Chronicle note, not a migration item.

  Chronicle's prefixes do not share the `C`/`P`/`L` numbering. Jokes in code are not release-note items (Guide §9.10).
- **Pre-tag checklist** (owner; also written into `docs/release-runbook-2.0.md`; RRM-2, RRM-3). On the exact commit `<sha>` to be tagged:
  1. `git merge-base --is-ancestor <sha> origin/master` succeeds, and the GitHub run for `<sha>` is green (`build`, `dreamtravel-tests`).
  2. Steps 02 and 04 are in `<sha>`:
     - `git show <sha>:src/Directory.Build.props | grep -c sol-technology` prints `2`;
     - `git show <sha>:.github/workflows/publishPackages.yml | grep -c "NuGet/login@"` prints `1`.
  3. `git show <sha>:src/Directory.Build.props | grep "<Version>"` shows `2.0.0`.
  4. The nuget.org policy is active, or temporarily active with its 7-day window still open. Restart the window if it has expired.
  5. No Dependabot PR has merged since the step 01 gate. If one has, check its floor lines in `CHANGELOG.md`.
  6. Tag exactly: `git tag -a v2.0.0 <sha> -m "SolTechnology.Core 2.0.0"`, then `git push origin v2.0.0`. Check that a `Build, test and publish` run starts for `v2.0.0`. A tag spelled `V2.0.0` or `2.0.0` starts no run, and `v2.0` fails the tag guard.
- **Failure handling** (RRM-7). `dotnet nuget push ./artifacts/*.nupkg` stops at the first failed package. The files go in name order, so `SolTechnology.Core.2.0.0.nupkg` is first and `SolTechnology.Core.Auth.2.0.0.nupkg`, the new ID, is about fifth. A policy that rejects a new ID therefore leaves a few packages published and the rest missing. To recover:
  - Fix the cause: the policy scope, the glob, `NUGET_USER`, or a pending ownership request.
  - Use **Re-run failed jobs** on the same run. The artifact is reused, a new OIDC token buys a new key, and `--skip-duplicate` skips what is already up.
  - Never delete and re-push the tag after any package reached nuget.org.
  - Never fall back to `NUGET_API_KEY` in the workflow to "get it out". The revocation below depends on Trusted Publishing having worked.
- **Revocation gate** (RRM-4). Revoke only when the `publish` job log for `v2.0.0` shows the `NuGet/login` step succeeded and every push used its key.

## Acceptance criteria
- [ ] The `publish` job succeeded for `v2.0.0`, and its log shows the `NuGet/login` step (link in Deviations).
- [ ] Each of the 20 package pages on nuget.org, including the new `SolTechnology.Core.Auth`, shows:
  - version 2.0.0;
  - its README, whose links are absolute and resolve;
  - the `sol-technology/SolTechnology.Core` repository link;
  - the authors "Adrian Strugala / Sol Technology".
- [ ] `docs/migration-2.0.md` has:
  - one section per confirmed `C-nn` that changes a signature, including `C-28`;
  - one section each for `P-03`, `P-04`, `P-05`, `P-06`, `P-07`, `P-17`, `P-18` and `P-27`;
  - the "Upgrade all packages together" section with the deprecated-ID table.
- [ ] `CHANGELOG.md` names no `C-25`, `C-30`, `C-31`, `P-21` or `P-22`, and has a Chronicle section.
- [ ] The `SolTechnology.Core.Authentication` page on nuget.org shows the deprecation with the alternate package `SolTechnology.Core.Auth`, and the `README.md` `SolTechnology.Core.Auth` badge shows 2.0.0 (owner-confirmed, dated in Deviations).
- [ ] `NUGET_API_KEY` no longer exists as a repository secret, and the key is revoked on nuget.org. Whether the unlist workflow ran before that is recorded (owner-confirmed, dated in Deviations).
- [ ] A fresh `dotnet new console` project outside the repository tree, with `dotnet add package SolTechnology.Core.CQRS --version 2.0.0`, builds the README quick start.

## Open questions
- none

## Deviations
