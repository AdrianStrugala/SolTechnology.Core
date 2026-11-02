---
status: planned
created: 2026-11-02
completed:
---

# SolTechnology.Core — Sol Technology org transfer and 2.0.0 release

> Historical delivery record. It may not describe the current system.

## Goal

Move `SolTechnology.Core` into the Sol Technology organization and publish 2.0.0 from there, so
the product site can present it next to AvroConvert. Success means:

- The repository lives at `https://github.com/sol-technology/SolTechnology.Core`, and the
  nuget.org organization `SolTechnology` co-owns every package ID.
- `LICENSE`, `<Copyright>`, and `<Authors>` name "Adrian Strugala / Sol Technology", and package
  metadata points at the organization repository.
- `master` has no ruleset (decision A, user, 2026-11-02): CI runs the test suite on push and PR,
  and direct pushes stay allowed. Secret scanning, push protection, and Private Vulnerability
  Reporting are enabled; the weekly stats bot still commits to `master`.
- Publishing uses NuGet Trusted Publishing; no long-lived `NUGET_API_KEY` remains.
- `SolTechnology.Core.*` 2.0.0 is published from tag `v2.0.0` with release notes and a migration
  guide.

## Context

**Depends on** (updated 2026-11-02 after the per-library regroup of the core feature): all of the
following briefs are `status: completed`. Step 01 pre-checks this before the transfer.

- [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) (foundation
  and umbrella);
- the 13 library features listed in its "Per-library regroup" section:
  [`2026-11-02-core-2-0-core-result`](2026-11-02-core-2-0-core-result.md),
  [`2026-11-02-core-2-0-logging`](2026-11-02-core-2-0-logging.md),
  [`2026-11-02-core-2-0-cqrs`](2026-11-02-core-2-0-cqrs.md),
  [`2026-11-02-core-2-0-auid`](2026-11-02-core-2-0-auid.md),
  [`2026-11-02-core-2-0-tale`](2026-11-02-core-2-0-tale.md),
  [`2026-11-02-core-2-0-http`](2026-11-02-core-2-0-http.md),
  [`2026-11-02-core-2-0-api-auth`](2026-11-02-core-2-0-api-auth.md),
  [`2026-11-02-core-2-0-cache`](2026-11-02-core-2-0-cache.md),
  [`2026-11-02-core-2-0-sql`](2026-11-02-core-2-0-sql.md),
  [`2026-11-02-core-2-0-blob`](2026-11-02-core-2-0-blob.md),
  [`2026-11-02-core-2-0-messagebus`](2026-11-02-core-2-0-messagebus.md),
  [`2026-11-02-core-2-0-hangfire`](2026-11-02-core-2-0-hangfire.md),
  [`2026-11-02-core-2-0-testing-kits`](2026-11-02-core-2-0-testing-kits.md);
- [`2026-11-02-flagship-showcase-hygiene`](2026-11-02-flagship-showcase-hygiene.md);
- [`2026-11-02-logging-chronicle`](2026-11-02-logging-chronicle.md).

### Origin

This feature was split from
[`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) on
2026-11-02 (RQ8, user: one premortem over ~84 steps delivered over months would rely on stale
evidence). Its steps are the core feature's post-review steps 78–82; that brief's "Split" table
maps the numbers.

### Evidence

The 2026-11-02 review against `master@4097d47f` found:

- `repo#A14`: `LICENSE:3` says "Copyright (c) 2026 Adrian Strugala", while the packages say
  `<Copyright>© 2026 Sol Technology</Copyright>` and `<Authors>Adrian Strugala</Authors>`
  (`src/Directory.Build.props:25,27`).
- `AdrianStrugala` appears 46 times in 12 files. `RepositoryUrl` and `PackageProjectUrl`
  (`src/Directory.Build.props:28,30`) drive SourceLink and the nuget.org links; updated URLs appear
  only on newly published versions.
- The review's `repo` org checklist requires the organization to enforce what the plan built:
  `master` protection (superseded by decision A, 2026-11-02), secret scanning, and push
  protection after the transfer (D1). Publishing
  still uses a long-lived `NUGET_API_KEY`, which core step 03 moves into the `nuget-release`
  environment.

### Decisions that apply (user, 2026-11-02)

- **D1 — Secrets.** The owner revokes the leaked keys (recorded by core step 06, former 09). Git history is
  not rewritten: no `git filter-repo`, no force-push. Org secret scanning and push protection are
  enabled after the transfer (step 03).
- **D2 — Versioning.** 2.0 is a major release. The confirmed public API changes (`C-01`…`C-24`,
  `C-26`, `C-27`; `C-25` rejected), behavioural and persisted-contract breaks (P-01…P-17), and log
  contract changes (L-01…L-05) are defined in the core brief and shipped by the core feature;
  step 05 publishes them.
- **D3 — Ordering.** The org transfer (GitHub transfer, nuget.org org co-owner, secrets, branch
  protection, URL updates) is the late phase, after the code and showcase work.

| ID | Decision | Steps |
|---|---|---|
| Q2 | Copyright holder and `Authors`: **"Adrian Strugala / Sol Technology"** in `LICENSE`, `<Copyright>`, and `<Authors>`. | 02 |
| Q3 | GitHub organization **`sol-technology`**; nuget.org organization **`SolTechnology`**. | 01, 02, 03, 04 |
| Q7 | The weekly NuGet stats bot keeps committing to `master`. No bypass actor is needed: `master` has no ruleset (decision A, 2026-11-02), and the workflow keeps pushing as today. | 02 |
| Q8 | Private Vulnerability Reporting is the `SECURITY.md` channel; `CODEOWNERS` is `* @AdrianStrugala`. | 03 |
| Q10 | NuGet Trusted Publishing (OIDC); no long-lived `NUGET_API_KEY`. | 01, 04 |
| RQ2 | **(c)** No `v2.0.0-rc.1` dry run; no tag-derived versioning. Residual risk AR-5. | 04, 05 |
| RQ7 | ~~**(a)** `master` ruleset: pull request and required checks, 0 approvals.~~ **Superseded by decision A (user, 2026-11-02):** "to nie włączaj ochrony brancha na master - tzn puszczenie test suite, ale wciąż możemy pushować bezpośrednio na master" — no `master` ruleset or branch protection; CI runs the test suite on push and PR; direct pushes to `master` stay allowed; `build` and `dreamtravel-tests` are not required checks. | 03 |
| RQ8 | Split the original feature; each part has its own premortem. | 00 |
| RQ11 | Post-review restructuring: license, authorship, and URLs merged into one `Directory.Build.props` PR after the transfer (step 02); protection split into repository settings (step 03; ruleset dropped by decision A, 2026-11-02) and Trusted Publishing (step 04). | 02, 03, 04 |
| RQ12 | Applied here: environment-before-merge ordering for Trusted Publishing (step 04) and the complete premortem trigger list (step 00). | 00, 04 |

### Related feature records

- [`2026-06-29-release-1-0.md`](2026-06-29-release-1-0.md) is the precedent for release mechanics
  (`docs/release-runbook-1.0.md`).

### Design alternatives

| # | Choice | Alternatives | Recommendation and crux |
|---|---|---|---|
| A8 | Weekly NuGet stats bot (`repo#A5`) | (a) commit to a `stats` branch; (b) drop the bot and use shields.io NuGet badges; (c) keep committing to `master` with a ruleset bypass | **(c)**, decided by the user (Q7, 2026-11-02). `CLAUDE.md §2` keeps reading `nuget-stats.json` from `master`. Since decision A (user, 2026-11-02) `master` has no ruleset, so the bot pushes directly without a bypass actor. |
| A11 | Publishing credential after the transfer | (a) a scoped `NUGET_API_KEY` for the organization; (b) NuGet Trusted Publishing (OIDC) | **(b)**, decided by the user (Q10, 2026-11-02). No long-lived secret; red team: first exercised by the real `v2.0.0` publish (AR-5). |

The A-numbers continue the core brief's table (A1–A10).

### Accepted risks

| ID | Risk | Source | Mitigation in this plan |
|---|---|---|---|
| AR-3 | The stats bot keeps writing weekly commits directly to `master`; `master` has no ruleset, so nothing but CI on push guards direct pushes (decision A, user, 2026-11-02). | Q7, decision A | No bypass actor exists. Step 03 verifies a stats-bot run after the read-only default `GITHUB_TOKEN` is set (the job declares `contents: write`). |
| AR-5 | The package version comes from `<Version>` in `src/Directory.Build.props`, not from the tag, so any `v*` tag on a `master` at 2.0.0 would publish the final 2.0.0 packages; Trusted Publishing is first exercised by the real `v2.0.0` release. | RQ2 | Publishing is tag-only through the protected `nuget-release` environment (core step 03); the required reviewer checks that the tag equals `<Version>` before approving (steps 04, 05); the interim `NUGET_API_KEY` is revoked only after the `v2.0.0` publish succeeds (step 05). |

The IDs keep their numbers from the core brief.

## Scope

### In scope

- Transfer the repository and set up the nuget.org organization (owner runbook) — step 01.
- License, authorship, and repository URLs, including both `Directory.Build.props` files — step 02.
- Security settings, repository presentation, nuget.org deprecation of the 8 deprecated IDs —
  step 03 (no `master` ruleset, decision A).
- NuGet Trusted Publishing — step 04.
- Release 2.0.0: `CHANGELOG.md` 2.0.0 section, `docs/migration-2.0.md`,
  `docs/release-runbook-2.0.md`, tag `v2.0.0`, revocation of the interim key — step 05.

### Out of scope

- Rewriting git history (D1).
- Moving the NuGet stats bot off `master` and changing `CLAUDE.md §2` (Q7).
- A `v2.0.0-rc.1` dry run or tag-derived versioning (RQ2).
- A `master` ruleset, branch protection rule, required status checks, or bypass actor (decision A,
  user, 2026-11-02).
- Rewriting historical feature records under `docs/features/` with new URLs.
- Any code change in `src/SolTechnology.Core.*` beyond package metadata (core feature).

## Implementation plan

Gates (see `summary.md`): review required — the feature changes package metadata, the publishing
credential, and the release path across every package. Premortem required — step 02 edits
`src/Directory.Build.props` and `sample-tale-code-apps/DreamTravel/Directory.Build.props`
(`CLAUDE.md §4`), and step 05 ships the core feature's persisted-contract breaks to consumers.

**Scope change 2026-11-02 (decision A, user):** "to nie włączaj ochrony brancha na master - tzn
puszczenie test suite, ale wciąż możemy pushować bezpośrednio na master". Step 03 no longer creates
a `master` ruleset, required checks, a stats-bot bypass actor, or the `update-nuget-stats.yml`
credential edit; RQ7 (a) is superseded. `review` stays `done (2026-11-02)`: the change only removes
user-decided scope and adds no step, file, contract, or dependency. `premortem` is still `pending`
and runs on this revised plan.

Notation: an unqualified step number is a step of this feature; `core NN` and `showcase NN` name
step `NN` of [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md)
and [`2026-11-02-flagship-showcase-hygiene`](2026-11-02-flagship-showcase-hygiene.md); library
short names (`result`, `auid`, `hangfire`, …) name the core 2.0 library features listed in "Depends
on". All those features' working folders are deleted before this feature starts, so steps here
cite their briefs (`Completion summary`, `Deviations`, and the core "Release-notes inputs"
section), not their step files.

| Step | Post-review core no. | Title | Notes |
|---|---|---|---|
| 00 | — | Run premortem | Gate; runs after review and after both prerequisite features are `completed`. |
| 01 | 78 | Transfer the repository and set up the nuget.org organization | Manual owner runbook. |
| 02 | 79 | License, authorship, and repository URLs | Runs after the transfer; must merge before `v2.0.0` is tagged. |
| 03 | 80 | Protect the org repository | Security settings and presentation only; no ruleset, bypass, or required checks (decision A). |
| 04 | 81 | Switch publishing to NuGet Trusted Publishing | Keeps the interim `NUGET_API_KEY` until step 05 succeeds (AR-5). |
| 05 | 82 | Release 2.0.0 | Release notes and migration guide from the core brief. |
| 06 | — | Retrospective | — |

## Acceptance criteria

- [ ] `git ls-remote https://github.com/sol-technology/SolTechnology.Core` succeeds, and
      `https://github.com/AdrianStrugala/SolTechnology.Core` redirects.
- [ ] The nuget.org organization `SolTechnology` co-owns every one of the 28 package IDs (20 live,
      8 deprecated).
- [ ] `LICENSE`, `<Copyright>`, and `<Authors>` name "Adrian Strugala / Sol Technology".
- [ ] `grep -rn "AdrianStrugala" --include=*.md --include=*.props --include=*.yml --include=*.csproj .`
      lists only intentional author attributions and historical feature records.
- [ ] `gh api repos/sol-technology/SolTechnology.Core/rulesets` returns `[]`: `master` has no
      ruleset, and direct pushes stay allowed (decision A).
- [ ] Secret scanning, push protection, and Private Vulnerability Reporting are enabled.
- [ ] `update-nuget-stats.yml` still commits to `master` after the read-only default
      `GITHUB_TOKEN` is set.
- [ ] `grep -n "secrets.NUGET_API_KEY" .github/workflows/publishPackages.yml` prints nothing, and
      `NUGET_API_KEY` is revoked on nuget.org (owner-confirmed, dated).
- [ ] `SolTechnology.Core.*` 2.0.0 packages are published from tag `v2.0.0` through NuGet Trusted
      Publishing, and their nuget.org pages show the README and the new repository URL.
- [ ] `docs/migration-2.0.md` has one section per confirmed `C-nn` and per P-03/P-04/P-05/P-06/P-07/P-17.

## Completion summary

## Deviations

## Follow-ups
