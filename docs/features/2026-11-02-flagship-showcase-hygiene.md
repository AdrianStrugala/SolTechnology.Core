---
status: completed
created: 2026-11-02
completed: 2026-11-10
---

# SolTechnology.Core — flagship showcase hygiene

> Historical delivery record. It may not describe the current system.

## Goal

Make the repository presentable as the Sol Technology organization's flagship before the org
transfer. Success means:

- A visitor arriving from the product site finds a root README quick start that compiles against
  2.0, plus `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`, issue and PR templates,
  `CODEOWNERS`, and `CHANGELOG.md`.
- Every GitHub Actions `uses:` is pinned to a commit SHA, Dependabot watches `nuget` and
  `github-actions`, and workflow comments are in English.
- DreamTravel, the reference app the Coding Guide cites, follows that guide: mediator-only thin
  controllers, a Domain that references nothing, a README that runs in two minutes, and tests that
  run in GitHub CI.
- `docs/ClaudeCodingGuide.md` and `CLAUDE.md` no longer contradict the code.

## Context

**Depends on** (rewritten 2026-11-02 after the per-library regroup of the core feature; former core
step numbers in parentheses):

- [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) steps
  02, 03, 04, 05 (08), and 07 (54) are `done`;
- [`2026-11-02-core-2-0-core-result`](2026-11-02-core-2-0-core-result.md) step 02 (38),
  [`2026-11-02-core-2-0-auid`](2026-11-02-core-2-0-auid.md) step 03 (44),
  [`2026-11-02-core-2-0-tale`](2026-11-02-core-2-0-tale.md) steps 03 (18), 07 (39), and 11 (43), and
  [`2026-11-02-core-2-0-api-auth`](2026-11-02-core-2-0-api-auth.md) step 05 (22; step 04 before the 2026-11-03 renumbering) are `done`.

None of these features needs to be `completed`. All of them are `done` or `completed` as of
2026-11-10 (see "Resync with the shipped 2.0 libraries" below). Notation: `core NN` names a step of the core
(foundation) feature; `result NN`, `auid NN`, `tale NN`, and `api-auth NN` name steps of the library
features above. Per step:

| Step here | Steps required | Why |
|---|---|---|
| 01 | core 04, result 02, core 07 | The README quick start compiles against the 2.0 version, the immutable `Result` (`C-02`), and the zero-dependency base package (P-13). |
| 02 | core 02, core 03 | Same workflow file; core 03 adds the `publish` job and the `actions/upload-artifact`/`actions/download-artifact` uses pinned here. |
| 03 | core 05, api-auth 05 | Core 05 adds the Production component test in the same test project; api-auth 05 makes `ResultConversionFilter` turn a failed `Result` into ProblemDetails. |
| 04 | core 05, tale 03, tale 11, auid 03 | `CLAUDE.md §11` rows must match tale 03, tale 11, and auid 03. Also requires step 03 here. Core 05 is `done`; the "dupa" CORS name stays as a kept joke (user, 2026-11-03), so step 04 no longer edits it. |
| 05 | tale 07 | Tale 07 edits the same `DreamTravel.Queries` chapter files. |
| 06 | — | None since 2026-11-02: core 05 (former 08) no longer adds `dotnet user-secrets` commands or edits `docker-compose.yml` (decision B, user, 2026-11-02: local-only DreamTravel dev credentials stay committed). |
| 07 | core 02, core 03 | Core 02 fixes the runner scope; core 03 creates the `build`/`publish` job layout the new job joins. Also requires step 02 here. |

[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md) depends
on this feature being `completed`.

### Origin

This feature was split from
[`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) on
2026-11-02 (RQ8, user: one premortem over ~84 steps delivered over months would rely on stale
evidence). Its steps are the core feature's post-review steps 71–77; that brief's "Split" table
maps the numbers.

### Evidence

A full-repository code review ran on 2026-11-02 against `master@4097d47f`. The review artifacts
live outside the repository, so this brief and its steps carry every finding they act on, with
`file:line`. Finding IDs used here:

| Prefix | Meaning |
|---|---|
| `P1-n` | P1 systemic items: `P1-1` docs drift, `P1-5` Polish text, `P1-6` Guide consistency, `P1-9` hygiene |
| `repo#An` | Repository and CI findings |
| `repo#Bn` | DreamTravel findings |
| `logging-cache#16` | Guide §12 requires the obsolete `SolTechnology.Core.Guards` |

### Boundary adjustment at the split

The user's recommended partition kept Guide self-consistency (post-review step 74) in the core
feature. It moved here as step 04 because it must run after step 03 (post-review 73): Guide §7
presents `CalculateBestPathController` as the `IMediator` template, and §15 lists controller
`try/catch` as a live example; only step 03 fixes both. Keeping it in the core feature would make
the core feature depend on this one while this one depends on the core feature. Its other
prerequisites (core 05, tale 03, tale 11, auid 03; former core 08, 18, 43, 44) point in the
allowed direction. Step 01's
`dontreadme.md` Guards row names the same BCL guards that step 04 writes into Guide
§12, so both now live in one feature.

### Decisions that apply (user, 2026-11-02)

| ID | Decision | Steps |
|---|---|---|
| Q1 | Keep `SolTechnology.Core.Guards`, `SolTechnology.Core.Scheduler`, `tests/SolTechnology.Core.Guards.Tests`, `docs/Guards.md`, `docs/Cron.md`. `C-25` rejected. The Guide §12 fix to BCL guards stays; `dontreadme.md` (kept under its name, user, 2026-11-03) names BCL guards for Guards. | 01, 04 |
| Q4 | No DreamTravel `UseNuGet=true` switch. The DreamTravel README documents the `PackageReference` equivalents. | 06 |
| Q7 | The weekly NuGet stats bot keeps committing to `master` as-is. This feature only pins its action and translates its comments. | 02 |
| Q8 | `SECURITY.md` channel: GitHub Private Vulnerability Reporting; Code of Conduct: Contributor Covenant 2.1; `CODEOWNERS`: `* @AdrianStrugala`. | 01 |
| Q9 | Keep the DreamTravel Azure DevOps pipelines, `devOps/scripts/`, and `devOps/infrastructure/legacy_AzureResourceGroup/` as a memento; do not delete. Only a new `dreamtravel-tests` GitHub Actions job is added. | 07 |
| RQ4 | Guards.Tests is not added to CI; Guards is reference-only. | 01 |
| RQ8 | Split the original feature; each part has its own premortem. | 00 |
| RQ11 | Post-review restructuring: former 72 + 74 merged into community health files and root README (step 01 here). | 01 |
| RQ12 | Applied here: `dontreadme.md` link repairs (01; void since 2026-11-03 — the file keeps its name, so no link changes), SHA pins and `needs` for the DreamTravel CI job (07), Guide self-consistency after the DreamTravel controllers (04). | 01, 04, 07 |

No public/protected symbol in `src/SolTechnology.Core.*` changes in this feature, so no `C-nn`
confirmation applies. The `IEvent` move into `SolTechnology.Core` stays "Not planned" (core brief).

### Design alternatives

| # | Choice | Alternatives | Recommendation and crux |
|---|---|---|---|
| H1 | Where `CitySearched` lives (`repo#B5`, step 05) | (a) move the record to `DreamTravel.Queries.Events`; (b) keep it in `DreamTravel.Domain` with the CQRS reference; (c) move `IEvent` into `SolTechnology.Core` | **(a).** (b) keeps breaking "Domain references nothing" (Guide §1/§6). (c) moves a public Core symbol and is "Not planned". Red team: DreamTravel stores `CitySearched` as a Hangfire job argument (`AddSolPersistentEvents()`), so the move changes a persisted type name. The premortem decides the mitigation: accepted without a type-name binder (SRM-14, 2026-11-10). |

### Accepted risks

| ID | Risk | Source | Mitigation in this plan |
|---|---|---|---|
| AR-4 | The DreamTravel Azure DevOps pipelines and legacy ARM folder stay without CI coverage. | Q9 | Step 07 runs DreamTravel tests in GitHub Actions. Their local-only credential values stay committed (decision B, user, 2026-11-02; core AR-6). |

The ID keeps its number from the core brief. The premortem's accepted risks (SRM-14 and the residuals of SRM-2, SRM-10, and SRM-17) are listed under "Premortem (2026-11-10)".

## Scope

### In scope

- Community health files and root README (`repo#A6`, `repo#A7`, `repo#A10`, `P1-9`) — step 01.
- Workflow supply-chain hardening (`repo#A6`, `repo#A8`, `repo#A15`, `P1-5`) — step 02.
- DreamTravel mediator-only controllers (`repo#B1`, `repo#B2`, `repo#B9`, `P1-1`) — step 03.
- Coding Guide self-consistency (`P1-6`, `logging-cache#16`) — step 04.
- DreamTravel logic and domain cleanup (`repo#B5`, `repo#B10`, `repo#B12`) — step 05.
- DreamTravel run instructions and an accurate architecture section (`repo#B7`, `repo#B8`) — step 06.
- DreamTravel tests in GitHub CI (`repo#B11`) — step 07.

### Out of scope

- Deleting the DreamTravel Azure DevOps pipelines, `devOps/scripts/`, or
  `devOps/infrastructure/legacy_AzureResourceGroup/` (Q9).
- A DreamTravel `UseNuGet` MSBuild switch (Q4).
- Moving the NuGet stats bot off `master` and changing `CLAUDE.md §2` (Q7).
- Moving `wroclaw_drive.graphml` (10.2 MB, used by `DreamTravel.Aspire/Program.cs`) and
  `research_documentation.docx` to Git LFS or release assets.
- Moving `IEvent` into `SolTechnology.Core` (core brief, "Not planned").
- Repository URLs, badges, authorship, and organization settings (release feature).
- Filling the `CHANGELOG.md` 2.0.0 section (release 05).

## Implementation plan

Gates (recorded in the working folder's `summary.md` until the retrospective): review required — the feature crosses root docs, CI workflows,
DreamTravel, and the Coding Guide. Premortem required — step 05 edits
`sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/ModuleInstaller.cs` and
moves a type that DreamTravel persists as a Hangfire job argument (`CLAUDE.md §4`). The workflow
edits in steps 02 and 07 are not a `CLAUDE.md §4` trigger by themselves.

**Scope change 2026-11-02 (user decisions A and B):** `dreamtravel-tests` runs on push and PR but
is not a required check, because `master` gets no ruleset (A); step 06 no longer depends on core
05 (former 08), because the local-only DreamTravel dev credentials stay committed (B). `review` stays
`done (2026-11-02)`: no step, file, contract, or dependency is added; `premortem` is still
`pending` and runs on this revised plan.

**Scope change 2026-11-03 (user: "nie dropuj joke z kodu"):** jokes in the code (humorous comments,
messages, names, file names) stay exactly as written (Guide §9.10). Step 01 keeps `dontreadme.md`
under its name and only edits its Guards row; the move to `docs/deprecated-packages.md` and the
link rewrites in `README.md`, `docs/Guards.md`, `docs/release-runbook-1.0.md`, and
`SolTechnology.Core.slnx` are dropped. Step 04 drops the "dupa" §15/§7 edits (already reflected in
the Guide). Steps 00 and release 01 reference `dontreadme.md` instead. `review` stays
`done (2026-11-02)`: scope is only removed.

Notation: an unqualified step number is a step of this feature; `core NN` and `release NN` name
step `NN` of [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md)
and [`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md);
`result NN`, `auid NN`, `tale NN`, and `api-auth NN` name steps of the core 2.0 library features
listed in "Depends on". The "Post-review core no." column below uses pre-regroup numbers.

| Step | Post-review core no. | Title | Notes |
|---|---|---|---|
| 00 | — | Run premortem | Gate; runs after review. |
| 01 | 71 | Community health files and root README | `SECURITY.md` names Private Vulnerability Reporting, enabled in release 03. `CHANGELOG.md` 2.0.0 section filled in release 05. |
| 02 | 72 | Workflow supply-chain hardening | After core 02 and 03. |
| 03 | 73 | DreamTravel — mediator-only, thin controllers | After core 05 and api-auth 05. |
| 04 | 74 | Coding Guide self-consistency | After step 03, core 05, tale 03, tale 11, and auid 03. |
| 05 | 75 | DreamTravel — logic and domain cleanup | Premortem-gated (`ModuleInstaller.cs`, persisted event type). |
| 06 | 76 | DreamTravel — run instructions and architecture section | No core prerequisite since decision B (2026-11-02). |
| 07 | 77 | DreamTravel — tests in GitHub CI | After step 02. Runs on push and PR; not a required check (decision A, user, 2026-11-02: no `master` ruleset). |
| 08 | — | Retrospective | — |

### Resync with the shipped 2.0 libraries (2026-11-10)

The plan was written on 2026-11-02, before the 13 library features, Chronicle, Redis fail-fast,
and foundation steps 01–05 and 07–10 shipped. The step files were compared with `master@c0eb9482`,
the briefs' Deviations, and `docs/architecture/*.md`. The Evidence section above stays as the
2026-11-02 record. No step is removed; two items were already delivered.

| Step | Drift at `c0eb9482` | Fixed in the step file |
|---|---|---|
| 01 | The `README.md:65-66` anchors resolve now (module-docs steps), but still skip the packed testing pages. The "Core (foundation)" row (`:39`) links to `docs/CQRS.md` although `docs/Core.md` exists (core-result). Community Standards includes settings-only items that need admin rights. | Link targets, `:39` added, owner-confirmed settings |
| 02 | Line numbers moved (unlist `:23`, publish `:20,22,87,102,107`); the stats workflow has a fifth Polish comment at `:60`. `docs/CICD.md` documents every workflow but was not listed. Core 03 shipped without the `nuget-release` environment. | Paths, `:60`, `docs/CICD.md` |
| 03 | v1 line numbers moved. The two new queries need `DreamTravel.Queries` → `DreamTravel.GraphDatabase`, which did not exist. The Swagger contract snapshot changes with the response types. Chronicle shipped before this step, inverting its planned order. | `.csproj` and snapshot added, Chronicle note |
| 04 | `AddAuthenticationAndBuildFilter` is already gone: **already delivered by api-auth 01** (`d46525f1`). `CLAUDE.md §11` rows are already accurate: **already delivered by auid 03** (`dd64b0d2`) and tale. New stale facts: §13 `AggregateError` namespace, §15 Newtonsoft row. | Auth edit dropped, `CLAUDE.md` is a check, §13 and §15 added |
| 05 | The Worker does not reference `DreamTravel.Queries` (the reference is new, not conditional); Worker `Program.cs:55` is now `:56`. The Domain reference removal breaks no transitive consumer. | Paths, reference made unconditional |
| 06 | The README grew to 519 lines with the Chronicle section (`:179-205`); the future-work item moved to `:516`. `docker-compose.yml` is stale against Aspire. | Line numbers, Chronicle section kept, compose deleted |
| 07 | The exact test projects were unnamed; `docs/CICD.md` was not listed; the throwaway-branch check cannot trigger on a plain branch push; `origin/master` is not a reliable diff base without branch protection. | Project list, `docs/CICD.md`, trigger and base |
| 08 | `package-release.md:12` is stale since core 03 but belongs to core 11. | Note added; Dockerfile follow-up |

### Premortem (2026-11-10)

Verdict: **go with mitigations** (step 00, run against `master@c0eb9482` after the resync above, in
a session that did not author the plan). The full record lived in step 00 of the working folder,
deleted by the retrospective (Git history at `811d2176`); this brief keeps what stays material.
Each mitigation was folded into its step file with its own acceptance criterion; the `SRM-` prefix
keeps them apart from the core feature's `RM-` and Chronicle's `CRM-` rows. Outcomes are under
"Completion summary".

| ID | Risk (scenario) | Sev | Mitigation | Step |
|---|---|---|---|---|
| SRM-1 | `SECURITY.md` promises Private Vulnerability Reporting before release 03 enables it. | M | Owner enables it on the current repository before `SECURITY.md` merges. | 01 |
| SRM-2 | A merged Dependabot PR raises a published dependency floor or takes a major; consumers get NU1605 and `canonical-versions.md` drifts. | H | No nuget majors; no group mixes library-floor and other packages; `Aspire*` grouped; dependency-floor rule in `CONTRIBUTING.md` and the PR template. | 01, 02 |
| SRM-3 | Split action bumps break only the tag-time `download-artifact`. | M | One `github-actions` group; artifact actions on one major. | 02 |
| SRM-4 | Step 02 checks pass falsely (`grep -P` on macOS) or have side effects (`UNLIST`, annotated-tag SHAs). | L | `grep -E`; empty `confirm`; dereferenced tag SHAs. | 02 |
| SRM-5 | v1 best path returns `CalculateBestPathResult` instead of `List<Path>` once it returns the `Result`. | M | Project to `BestPaths`; v1 component test. | 03 |
| SRM-6 | v1 LimitCost reads an unconfigured Session; without `try/catch` every call is a 500 with a Critical log. | M | Body-bound query (decision); component tests. | 03 |
| SRM-7 | v2 controllers already on `IMediator` drop the request token. | L | `HttpContext.RequestAborted` everywhere; grep. | 03 |
| SRM-8 | Kept jokes ("dupa", "DUPA.", "Fire!", `//I'm just void`) or report-only log templates are rewritten. | M | Verbatim; grep checks; report-only templates listed under Deviations. | 03, 04, 05 |
| SRM-9 | The Guide template and controllers declare the `Result` envelope that the filter removes. | M | Declare the unwrapped type and ProblemDetails. | 03, 04 |
| SRM-10 | The UI Aspire starts cannot talk to the API (envelopes, `/api/v2` routes, base URL, Maps key). | M | README does not present the UI as working; follow-up. | 06, 08 |
| SRM-11 | Clean clone never starts: the dacpac is not built; amd64 SQL Server on Apple silicon. | M | Build before run; prerequisites; check from a fresh clone. | 06 |
| SRM-12 | Google-backed endpoints point at an unserved `localhost:2137`. | M | Google-free smoke calls; optional own-key section. | 06 |
| SRM-13 | The README rewrite drops the Chronicle section. | L | Section kept; diff check. | 06 |
| SRM-14 | Stored `CitySearched` jobs with the Domain type name fail to load after the move. | L | Accepted (no binder); remedy recorded. | 05 |
| SRM-15 | Guide §0/§1/§6 and the `command-query-event-tale` skill still put `IEvent` records in the Domain. | M | Same-PR edits; grep. | 05 |
| SRM-16 | A list copy still mutates the caller's `Path` objects. | L | Copy elements; test asserts element values. | 05 |
| SRM-17 | The first Linux run of `dreamtravel-tests` fails and blocks the tag publish; the job is weakened to unblock it. | M | `timeout-minutes`, no `continue-on-error`/`if:`; test-side fixes; re-run rule in `docs/CICD.md`. | 07 |
| SRM-18 | The quick-start check passes only inside the repository (root props, CPM). | L | Throwaway project outside the tree, local pack feed. | 01 |
| SRM-19 | The v1 `LogDetail` path never matches the header-versioned route. | L | Path fixed. | 03 |

Decisions recorded under the owner's pre-approval (2026-11-10; no confirmation stop):

- SRM-6: v1 LimitCost binds its paths from the body; the Session dependency goes.
- SRM-10: the UI is not fixed in this feature; the README is honest about it, and step 08 records
  the follow-up.
- SRM-14: no type-name binder for `CitySearched` (H1 stays (a)).
- Step 05: the currency symbol becomes `€`. Step 06: `docker-compose.yml` is deleted; the
  database `Dockerfile` stays (decision B) and becomes a follow-up.

Owner actions that need admin rights (manual, outside the repository):

- Before step 01 merges: enable Private Vulnerability Reporting (SRM-1).
- For the Community Standards criterion: set the repository description and accept content
  reports.
- After step 02 lands: run each workflow once with `workflow_dispatch` on a branch — the unlist
  workflow with an empty `confirm` (its job must be skipped; never type `UNLIST`), the stats
  workflow on a throwaway branch that is deleted afterwards (it commits `nuget-stats.json` there).
- After step 02 merges: check the first Dependabot run (Insights → Dependency graph → Dependabot)
  and that Dependabot security updates are on; create the `dependencies` label if Dependabot
  reports it missing; optionally require full-length SHA pins for actions (Settings → Actions).
- Step 06: run the fresh-clone check on the owner's machine (Docker Desktop with amd64
  emulation on Apple silicon).
- Step 07: watch the first `dreamtravel-tests` run on a PR or `master` push (the agent rehearsed
  it only in a Linux arm64 container); for the forced-failure check, add `Assert.Fail()` to a
  DreamTravel unit test on a throwaway branch, run the workflow with `workflow_dispatch` on it, see
  the job fail, and delete the branch.

## Acceptance criteria

Checked at the retrospective (2026-11-10, `master@8df6a849` plus this closing commit). The two
unchecked items need the owner; see "Owner-only acceptance checks" under Deviations.

- [x] The root `README.md` has a quick start that builds in a throwaway project against the 2.0
      packages, and `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`, `CHANGELOG.md`, and
      `.github/dependabot.yml` exist.
- [ ] The GitHub "Community Standards" page shows every item checked. *Every file item exists;
      the description, content reports, and Private Vulnerability Reporting are owner settings.*
- [x] `dontreadme.md` exists at the repository root under its name, and its Guards row names the
      BCL guards.
- [x] `grep -rnE "uses: [^@]+@v[0-9]" .github/workflows` prints nothing.
- [x] `grep -rnE "IQueryHandler<|ICommandHandler<|CancellationToken\.None|catch \(" sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Controllers`
      prints nothing.
- [x] `grep -n "SolTechnology.Core.CQRS" sample-tale-code-apps/DreamTravel/src/DreamTravel.Domain/DreamTravel.Domain.csproj`
      prints nothing.
- [x] `grep -nE "SolTechnology\.Core\.Guards|AddAuthenticationAndBuildFilter|ResponseEnvelopeFilter" docs/ClaudeCodingGuide.md`
      prints nothing.
- [x] Following the DreamTravel README on a clean clone starts the Aspire dashboard and the API
      (manual check, recorded in step 06: 2026-11-10, macOS 26 arm64, Docker Desktop with Rosetta,
      fresh clone of `fb6fc348`). *The owner's interactive run stays an owner action.*
- [ ] The `dreamtravel-tests` job runs green on a PR, and the `publish` job declares
      `needs: [build, dreamtravel-tests]`. *`needs` is in place; the job's script ran green in a
      Linux container, and the first GitHub run waits for the owner's push.*
- [x] `sample-tale-code-apps/DreamTravel/devOps` is unchanged by this feature (Q9;
      `git diff --stat b244b728 -- sample-tale-code-apps/DreamTravel/devOps` prints nothing).
- [x] Every SRM mitigation ("Premortem (2026-11-10)") is checked in its step file or recorded as a deviation.

## Code review (2026-11-10)

An independent, read-only review of steps 01–07 (`4901e55b..811d2176`) had one finding.

| ID | Sev | Finding | Fix | Commit |
|---|---|---|---|---|
| R1 | M | `4e4b6bb9` regressed v1 `FindLocationOfCity` for an unknown city from 400 to 500: the controller's `try/catch` was gone, and the Google adapter reported `ZERO_RESULTS` as a generic error. | The Google adapter returns `NotFoundError` on `ZERO_RESULTS`, so v1 and v2 city lookups answer 404 ProblemDetails; component tests and the contract snapshot cover it. | `13484d76` |

The review confirmed the rest: workflow SHA pins, `permissions`, and that `publish` needs
`dreamtravel-tests`; Dependabot groups that respect SRM-2; cancellation in every controller and
the reviewed OpenAPI diff; the Domain cleanup; docs, links, and kept jokes. DreamTravel component
tests were 40/40 at review time and are 44/44 after the post-step commits.

## Completion summary

Delivered on 2026-11-10 in `4901e55b`…`811d2176`, followed by the post-step fixes below. The
repository now has the community files, a pinned supply chain, a DreamTravel that follows the
Coding Guide and runs from a fresh clone, and DreamTravel tests that gate the tag publish.

- Step 01 (`4901e55b`): `CONTRIBUTING.md` (with the dependency-floor rule), `SECURITY.md`,
  `CODE_OF_CONDUCT.md` (Contributor Covenant 2.1), issue and PR templates, `CODEOWNERS`,
  `CHANGELOG.md`, and a root README quick start that built and answered `GET /hello/Tale` in a
  project outside the tree against a local pack feed. `dontreadme.md` keeps its name; its Guards
  row names the BCL guards.
- Step 02 (`6fd9528f`): every action pinned to the commit its `v4` tag pointed to (no behaviour
  change), Dependabot for `nuget` (majors ignored, floor groups kept pure) and `github-actions`
  (one group), English workflow comments, and `docs/CICD.md` on pins, groups, and the eight unlist
  ghost ids.
- Step 03 (`98ea97f6`, `4e4b6bb9`): every DreamTravel controller sends through `IMediator` with
  `HttpContext.RequestAborted`; no handler injection, no `try/catch`. v1 keeps its array body,
  `LimitCost` binds its body and gained a validator, `ProjectsController` declares unwrapped types,
  and the v1 `LogDetail` path matches. The Swagger diff was reviewed item by item.
- Step 04 (`e000ab10`): the Coding Guide no longer names Guards, `AddAuthenticationAndBuildFilter`,
  or `ResponseEnvelopeFilter`; §7 declares unwrapped types, §13 and §15 match the code, and the
  DreamTravel build and test commands in `CLAUDE.md`, two skills, and `CONTRIBUTING.md` work.
- Step 05 (`fe33fe34`): `DreamTravel.Domain` references nothing; `CitySearched` lives in
  `DreamTravel.Queries.Events` (H1 (a), SRM-14 accepted); the cost-limit handler copies paths; the
  currency symbol is `€`. Guide §0/§1/§6 and the `command-query-event-tale` skill teach the
  LogicLayer `Events/` folder.
- Step 06 (`acdeca53`, `fb6fc348`, `61d7fced`): a DreamTravel README that runs from a fresh clone
  (build the dacpac, then the AppHost), Google-free smoke calls, an own-key section, honest
  limitations for the UI, and an accurate architecture section; `docker-compose.yml` is gone. The
  clean-clone check ran on 2026-11-10 on macOS 26 arm64 with Docker Desktop and Rosetta, from a
  fresh clone of `fb6fc348`: the API answered `/healthz` and `/api/statistics/countries` with 200
  after about 75 s, with the two `ASPIRE_*` fallback variables.
- Step 07 (`aeeb1358`, `811d2176`): the `dreamtravel-tests` job runs six DreamTravel test projects
  on every trigger with `timeout-minutes: 30` and no `if:` or `continue-on-error`; `publish` needs
  it; `docs/CICD.md` documents the job and the re-run rule.
- Architecture at close: `package-release.md` (pinned supply chain, Dependabot, the DreamTravel
  gate on publish), `testing.md` (DreamTravel tests in CI, process-wide Hangfire state across test
  hosts), and `background-processing.md` (schema install at start for enqueue-only hosts, stored
  event argument types). `http-client.md` already describes P-30 from `ba8231ec`.

### Post-step work (2026-11-10)

These commits landed on `master` after step 07 and belong to this feature:

- `26eeadd6` `perf(dreamtravel)`: the API resolves Hangfire's `JobStorage` at start, so the schema
  install no longer runs inside the first request that publishes an event. The short-lived
  Production test host now reuses `JobStorage.Current`; it had rebound Hangfire's process-wide
  logging to a logger factory it disposed.
- `13484d76` `fix(dreamtravel)`: an unknown city answers 404 instead of 500 (code review R1).
- `e84a4943` `fix(dreamtravel)`: one resilience pipeline on the Sol clients. Aspire's
  `ConfigureHttpClientDefaults` stacked the standard handler on top of `core-http-{name}`; a
  component test walks the handler chains of the API and the Worker.
- `ba8231ec` `fix(http)!`: the library owns resilience on Sol clients when `UsePolly` is on (P-30
  in the core brief), so DreamTravel is back on the standard Aspire template line and its
  component test still finds one `ResilienceHandler` per Sol client.
- `8df6a849` `docs(dreamtravel)`: the Chronicle report examples in the DreamTravel README were
  re-run, including the 404 report.

Related but separate: `f9d0111c` `feat(logging)` names Chronicle SQL steps after their target
table; it has its own record,
[`2026-11-10-chronicle-sql-step-targets`](2026-11-10-chronicle-sql-step-targets.md).

### SRM outcomes

| ID | Outcome |
|---|---|
| SRM-1 | Open, owner action: enable Private Vulnerability Reporting before pushing (the GitHub API shows it disabled). `SECURITY.md` falls back to `contact@soltechnology.dev`, so it promises nothing that is off. |
| SRM-2 | Done: majors ignored, floor groups pure, rule in `CONTRIBUTING.md` and the PR template. Residual: follow-up review after the first month. |
| SRM-3 | Done: one `github-actions` group; artifact actions on major 4. |
| SRM-4 | Done for what the agent may run (`grep -E` in a UTF-8 locale, dereferenced tag SHAs); the `workflow_dispatch` runs are owner actions. |
| SRM-5 | Done: v1 best path returns a JSON array; component test. |
| SRM-6 | Done: `LimitCost` binds its body; validator and component tests. |
| SRM-7 | Done: every `Send` passes `HttpContext.RequestAborted`. |
| SRM-8 | Done: "dupa", "DUPA.", "Fire!", `//I'm just void`, and the report-only templates are unchanged. |
| SRM-9 | Done: controllers and the Guide declare unwrapped types and ProblemDetails. |
| SRM-10 | Done as decided: the README is honest about the UI; alignment is a follow-up. |
| SRM-11 | Done: build-then-run, prerequisites, fresh-clone check. |
| SRM-12 | Done: Google-free smoke calls and an own-key section. |
| SRM-13 | Done in step 06; the section was refreshed later by `8df6a849`, on purpose. |
| SRM-14 | Accepted: no binder; stored jobs with the Domain type go to Failed and are deleted by hand. |
| SRM-15 | Done in the same commit as the move. |
| SRM-16 | Done: elements copied; the test asserts input values. |
| SRM-17 | Done: `timeout-minutes`, no `if:` or `continue-on-error`, re-run rule; the first GitHub run is the owner's check. |
| SRM-18 | Done: quick start built outside the tree against a local pack feed. |
| SRM-19 | Done: `LogDetail` path fixed and tested. |

### Retrospective

- Planned versus delivered: every step met its contract; the material differences are under
  Deviations. The seams held: step 03's controller shape is what step 04 documents in Guide §7,
  step 05's event move is what Guide §6 and the skill teach, and step 07's job is what
  `docs/CICD.md` and the release re-run rule describe.
- What worked: verifying by real runs. The quick start built outside the tree, the fresh clone
  found the keychain and Neo4j start-up traps that reading the README could not, and the Linux
  container rehearsal ran the job script before GitHub did.
- What the plan missed: removing the controllers' `try/catch` changed which errors become which
  status codes (R1, found only by the independent review); the Aspire default resilience handler
  stacked on Sol clients (found in a Chronicle trace, fixed in the library as P-30); and Hangfire's
  process-wide logging leaked between test hosts. The lessons are in the premortem references
  (`api.md`, `http.md`, `hangfire.md`) and in a new `ci-docs-and-samples.md` for workflows, docs
  with runnable examples, and the sample app.
- Validation at close: docs-only retrospective, so no build or test run; the feature's code was
  last verified by the post-step commits (DreamTravel component tests 44/44) and the acceptance
  greps above were re-run on 2026-11-10.

## Deviations

### Implementation decisions (owner pre-approval, 2026-11-10)

Decided during implementation without a confirmation stop; each step file carries the detail.

- Step 01: `SECURITY.md` and `CODE_OF_CONDUCT.md` use the public `contact@soltechnology.dev`
  address, as the Private Vulnerability Reporting fallback and as the enforcement contact, so the
  files promise nothing that is not enabled yet (SRM-1).
- Step 02: actions are pinned to the commit their moving `v4` tag pointed to on 2026-11-10, not to
  a newer major, so the pins change no behaviour; Dependabot proposes later versions.
- Step 03: `LimitCostOfPathsQuery` gets a validator (`CostLimit >= 0`, non-empty `Paths`) so SRM-6
  has a validation-failure path. The v1 contract changes (ProblemDetails failures, `LimitCost` body)
  are listed in the DreamTravel README; DreamTravel has no changelog of its own.
- Step 03 found three defects it does not fix (follow-ups for step 08): a single-city best-path
  request returns 200 `[]` because `Otherwise<JustOrderCities>()` recovers the `Expect` loss;
  `CreateRequest(...).WithBody(...)` sends no `Content-Length`, so `LogDetail` body rules never fire
  for it; `api/Traffic/recalculate` has no handler (commented out) and declares the command as its
  response type.
- Step 04: `CLAUDE.md §3`, two skills, and `CONTRIBUTING.md` told readers to run `dotnet build` or
  `dotnet test` inside `sample-tale-code-apps/DreamTravel`, which has no solution file; they now
  name `SolTechnology.Core.slnx` and per-project test paths.
- Step 05 (SRM-14, accepted): `CitySearched` moved to `DreamTravel.Queries.Events`. Hangfire jobs
  stored before the move name `DreamTravel.Domain.Events.CitySearched, DreamTravel.Domain` and go
  to Failed; only local databases hold them. Remedy: delete them under Jobs → Failed in the
  Worker's Hangfire dashboard (`/hangfire/ui`) or drop the local database volume.
- Step 06: the fresh-clone check ran on macOS arm64 from the agent's non-interactive shell, where
  the keychain prompt for the development certificate cannot be answered. The README therefore
  names two `ASPIRE_*` variables as the fallback, and a restart for a Neo4j import that ran before
  Bolt was ready. The owner's interactive run and an import into an empty Neo4j volume stay an
  owner action.

### Promoted from the step files (2026-11-10)

- Step 02: the Polish-comment grep exits 1 only in a UTF-8 locale; in the C locale BSD grep
  matches the kept `📊` emoji byte by byte. `docs/CICD.md` also gained the two missing unlist ghost
  ids (`Faker`, `Flow`).
- Step 03: v1's failure case is a 400 ValidationProblemDetails for `{"cities": null}`, because a
  single-city request returns 200 `[]` (follow-up). The SRM-19 test posts a `StringContent` body,
  because `CreateRequest(...).WithBody(...)` sends no `Content-Length` and the logging middleware
  buffers only bodies with a length (follow-up). The `LimitCost` logger category changed to
  `LimitCostOfPathsController` (Guide §11).
- Step 05: `LimitCostOfPathsTests.cs` became `LimitCostOfPathsHandlerTests.cs` to match the
  renamed handler.
- Step 07: the job runs `dotnet test` per project in a loop that keeps going after a failure and
  names every failed project in one `::error::` line; it does not rebuild the whole solution.
- Post-step: the 404 for an unknown city (R1) and the single resilience pipeline (P-30) changed
  DreamTravel's behaviour after step 07; the README and the contract snapshot follow.

### Owner-only acceptance checks

**Original plan:** the feature closes with the Community Standards page fully checked and a green
`dreamtravel-tests` run on a PR.

**Actual implementation:** both need the owner (admin settings, a push, and workflow runs), and
the agent may do neither. The owner pre-approved closing on 2026-11-10; the checks are listed under
"Owner manual actions", and release 01 re-checks them before the transfer.

## Follow-ups

- SRM-10: align `DreamTravel.Ui` with the API wire format: unwrapped bodies and ProblemDetails
  instead of `Result<T>`, header versioning instead of `/api/v2/...`, the `X-API-KEY` header, and
  the API base URL from Aspire.
- SRM-2 residual: after the first month of Dependabot, check that floor PRs carried their
  `canonical-versions.md` and `CHANGELOG.md` lines.
- `CalculateBestPathTale`: a single-city request returns 200 `[]` because
  `Otherwise<JustOrderCities>()` recovers the `Expect(NoOfCities > 1)` loss.
- `SolTechnology.Core.HTTP` and Logging: `CreateRequest(...).WithBody(...)` sends `JsonContent`
  without `Content-Length`, so `LogDetail` body rules never fire for such requests.
- DreamTravel `api/Traffic/recalculate`: its handler is commented out and the action declares the
  command as its response type; remove it or restore the handler.
- `docs/theDesign.md:66-69` still shows `ExceptionFilter` and `ResponseEnvelopeFilter` on a
  controller.
- `src/Infrastructure/DreamTravelDatabase/Dockerfile` is orphaned since `docker-compose.yml` was
  deleted (kept under decision B).
- Git LFS or release assets for `wroclaw_drive.graphml` (10.2 MB) and `research_documentation.docx`.
- CI coverage or retirement of the DreamTravel Azure DevOps pipelines (AR-4, Q9).
- Owned elsewhere, not repeated here: `docs/architecture/package-release.md` still states version
  1.0.0 and a `workflow_dispatch` publish (core 11), and the `docs/CICD.md` "Versioning policy"
  section is stale (release feature).

## Owner manual actions

Collected from the steps; none can be done by the agent.

1. Before pushing: enable Private Vulnerability Reporting (Settings → Code security; the GitHub
   API shows it disabled), so `SECURITY.md`'s first channel works (SRM-1).
2. Set the repository description, and turn on content reports (Settings → Moderation options → Reported content),
   then confirm Insights → Community shows every item checked.
3. After the push: run each workflow once with `workflow_dispatch` on a branch: the package
   workflow (publish skipped), the unlist workflow with an empty `confirm` (its job skipped; never
   type `UNLIST`), and the stats workflow on a throwaway branch, deleted afterwards because the
   workflow commits `nuget-stats.json` there (SRM-4).
4. Check the first Dependabot run (Insights → Dependency graph → Dependabot), that Dependabot
   security updates are on, and create the `dependencies` label if Dependabot reports it missing.
   Optionally require full-length SHA pins for actions (Settings → Actions → General).
5. Watch the first `dreamtravel-tests` run on a PR or a `master` push (Linux x64; the agent
   rehearsed it only on Linux arm64). If it fails on Linux only, fix tests or fixtures and re-run.
6. Forced-failure check: add `Assert.Fail()` to a DreamTravel unit test on a throwaway branch, run
   the workflow with `workflow_dispatch` on it, see `dreamtravel-tests` fail, then delete the
   branch.
7. Fresh-clone check on the owner's Mac: follow the DreamTravel README interactively (answer the
   keychain prompt instead of the `ASPIRE_*` fallback) and let the Neo4j import run into an empty
   volume.
8. After about a month: the SRM-2 residual review under Follow-ups.
