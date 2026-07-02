# 013-Release 1.0 — Implementation Summary

Tracking the implementation steps for the spec [`../013-release-1.0.md`](../013-release-1.0.md).

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 00 | Run premortem (gate) + resolve open questions | [`done/00-run-premortem.md`](done/00-run-premortem.md) | ✅ done — **Go with mitigations** |
| 01 | CI: gate publish behind release trigger + pack-by-glob | [`done/01-ci-publish-gate-and-glob-pack.md`](done/01-ci-publish-gate-and-glob-pack.md) | ✅ done |
| 02 | Centralise NuGet metadata + SourceLink + README wiring (docs-sourced, no version change) | [`done/02-nuget-metadata-and-sourcelink.md`](done/02-nuget-metadata-and-sourcelink.md) | ✅ done |
| 03 | Rename wave 1 — Logging surface (+ Api/HTTP callers) | [`done/03-rename-logging-surface.md`](done/03-rename-logging-surface.md) | ✅ done |
| 04 | Rename wave 2 — data + transport modules | [`done/04-rename-data-and-transport-modules.md`](done/04-rename-data-and-transport-modules.md) | ✅ done |
| 05 | Rename wave 3 — Api + Authentication (rename only) | [`done/05-rename-api-authentication.md`](done/05-rename-api-authentication.md) | ✅ done |
| 05b | Tale rebrand + package rename (`Story` → `Tale`) + route/controller/snapshot (13c) | [`done/05b-tale-rebrand-and-package-rename.md`](done/05b-tale-rebrand-and-package-rename.md) | ✅ done |
| 06 | Fix Authentication `BuildServiceProvider` anti-pattern (+ new test project) | [`done/06-authentication-antipattern-fix.md`](done/06-authentication-antipattern-fix.md) | ✅ done |
| 07 | Deprecate `Scheduler` + `Guards` in source (+ `Story` ghost via runbook) | [`reviewed/07-deprecate-scheduler-and-guards.md`](reviewed/07-deprecate-scheduler-and-guards.md) | 🔍 reviewed |
| 08 | Flip shared version to `1.0.0` (Logging `1.2.0`; Tale first at `1.0.0`) | [`reviewed/08-version-flip-1.0.md`](reviewed/08-version-flip-1.0.md) | 🔍 reviewed |
| 09 | README + `HTTP`-successor + `Tale` row parity (owns `Clients.md`) | [`reviewed/09-readme-http-successor-parity.md`](reviewed/09-readme-http-successor-parity.md) | 🔍 reviewed |
| 10 | Migration guide + CHANGELOG + runbook + `dontreadme` + `CICD.md` (incl. `Story → Tale`) | [`reviewed/10-migration-runbook-changelog.md`](reviewed/10-migration-runbook-changelog.md) | 🔍 reviewed |
| 11 | Doc integrity sweep + `Story.md → Tale.md` + skill rename → `command-query-event-tale` | [`reviewed/11-doc-integrity-sweep.md`](reviewed/11-doc-integrity-sweep.md) | 🔍 reviewed |

Status values: `⬜ to-do` / `🔍 reviewed` / `✅ done`. Link in each row points to the step's current
location (`to-do/` / `reviewed/` / `done/`). All 13 steps live in `reviewed/`. Step `05` was **split**
on 2026-06-30 into `05` (Api + Authentication) + new `05b` (the `Story → Tale` rebrand + package
rename) when the accepted "Tale noun" decision made the Story rename the single largest in the plan.
Step `00` is the premortem gate ([ADR-006 §5](../006-implementation-plan-workflow.md)) — it runs first
and blocks `01..11` (incl. `05b`) until it returns *Go* / *Go with mitigations*. Its open
sub-questions are now **answered** (`13a` → B2, `13b` → rename the skill, `13c` → route to `api/tale`
with breaking accepted) and propagated into `05b` + `11`; the only item left for the gate is its own
execution (the Go / No-Go verdict).

## Sequencing rationale

Risky / foundational first, then docs:

- **01–02 (safety rails):** the publish gate lands *before* any rename so the incremental breaking
  PRs cannot auto-publish a broken `0.x`; metadata centralisation carries **no** version change.
  Step 02 also **centralises the existing per-package README wiring** (each package already ships a
  README sourced from `docs/*.md`; answer 9's "~20 new `src/` READMEs" is dropped as duplication per
  the 2026-07-01 amendment) and fills the three missing docs (`Core`, `API.Testing`, `SQL.Testing`) in
  `docs/`; it carries **no** version change.
- **03–06 (the breaking rename):** ordered callee-before-caller — Logging first (its only Core callers
  are Api + HTTP, fixed in the same PR), then data/transport, then top-level Api + Auth (05). The
  **`Story → Tale` rebrand + package rename is its own step (05b)** — the largest single rename
  (new package identity + ~15 public types + `…Story.Tale` namespace collapse + tests + sample), done
  in **one wave** because a package/namespace rename cannot go callee-before-caller across assemblies.
  Each wave updates every call site **and sweeps the renamed symbol out of XML-doc / comments /
  exception+log strings** so `dotnet build SolTechnology.Core.slnx` stays green and tests asserting on
  those strings keep passing. The governing **prose** for Tale (the `command-query-event-story` skill —
  **renamed to `command-query-event-tale`** per 13b, `ClaudeCodingGuide.md` §0/§3/§4, `CLAUDE.md`,
  `docs/Story.md → Tale.md`, diagrams) is **not** in 05b
  — it does not affect build-greenness and the skill cites the guide (§19), so it is reconciled
  together in the step-11 doc pass. The Authentication logic fix is isolated (06) from the mechanical
  rename (05) and **runs** (answer 7 = fix); it adds the new `SolTechnology.Core.Authentication.Tests` host.
- **07–08 (deprecate + go-live version):** `[Obsolete]` the source-bearing deprecated libs and **stop
  packing them now** (answer 10 — already outside `.slnx`); the renamed **`Story` package** has no
  `[Obsolete]`-able source under its old id, so it is deprecated + unlisted on nuget.org via the
  step-10 runbook (like `ApiClient`). Then the shared `1.0.0` flip (Logging `1.2.0`; **`Tale` ships
  its first version at `1.0.0` — a clean new id, no downgrade**) as the last code change before a
  deliberate release.
- **09–11 (docs + parity):** README/`HTTP` reconciliation + the `Story Framework → Tale Framework`
  row/badge/hero (step 09 **owns** the `Clients.md → HTTP` disposition), migration/runbook/CHANGELOG/`CICD.md`
  (incl. the `Story → Tale` migration table + `dontreadme` row), then the `documentation-cleanup`
  integrity sweep last — which **owns** the `Story.md → Tale.md` content move + the `ClaudeCodingGuide`/`CLAUDE.md`/skill/diagrams
  prose pass, and only **verifies** the `Clients.md` links.

## Review

Reviewed by the `plan-reviewer` agent on 2026-06-30. All 12 open questions from step `00` were
answered by the maintainer and folded into steps `01–11`; blockers (missing health checks in the
rename scope, the missing Authentication test host) and majors (repo-wide symbol-string sweep, the
Worker blast radius, the slnx-glob "forgot CI" guard) are resolved in the reviewed drafts. The
premortem gate (`00`) still has to be **executed** (skill run + Go verdict) before any step ships.

**Amendment 2026-06-30 — `Story → Tale` decision integrated.** The accepted "Tale noun + package
rename" decision (`SolTechnology.Core.Story → SolTechnology.Core.Tale`, `StoryHandler → TaleHandler`,
`RegisterStories → AddSolTale`, `…Story → …Tale` suffix) was folded in: new code step **05b** (step 05
split into Api/Auth + the Tale rebrand), plus updates to `00` (decision 13 + failure mode 8 + open
sub-questions 13a/13b/13c), `01` (glob packs Tale, never re-packs Story), `07` (Story ghost routed to
the runbook), `08` (`Tale` first at `1.0.0`), `09` (Tale README row + hero), `10` (Story→Tale migration
table + deprecate/unlist + `dontreadme` row), and `11` (owns the `Story.md → Tale.md` + guide / `CLAUDE.md`
/ skill / diagrams prose pass — including governing docs the decision's own blast-radius had omitted).
Open for the gate: **13a** B1/B2 (recommend B2), **13b** skill rename (recommend rename), **13c** base
route (recommend keep).

**Amendment 2026-06-30 — sub-questions `13a`/`13b`/`13c` answered.** The maintainer confirmed
**13a → B2** (full `Tale*` rebrand + `…Story.Tale` namespace collapse — 05b needed no structural
change), **13b → rename the skill** `command-query-event-story → command-query-event-tale` (folder +
heading + every cross-ref; owned by step 11), and **13c → change the route, breaking accepted** —
`[api/story] → [api/tale]`, sample `DreamTravelStoryController → DreamTravelTaleController` +
`[api/dreamtravel/story] → [api/dreamtravel/tale]`, `SQLiteStoryRepository → SQLiteTaleRepository`,
`SaveCityStory → SaveCityTale`, all hard-coded test URL strings swapped, and the lone component
`*.verified.txt` contract snapshot **regenerated on purpose** (folded into step 05b). Failure mode 8c
in step `00` is reversed accordingly (snapshot now churns by design). The gate stays **unexecuted** —
recording these answers does not produce the Go / No-Go verdict. Residual flag (surfaced, not blocking):
the Core public method names `StartStory`/`ResumeStory`/… and the `{storyId}` route-template token keep
their `Story` spelling — a Core-API rename beyond the B2 type/namespace rebrand, left to a separate
maintainer call.

**Amendment 2026-06-30 — ghost-package retirement is CI-automated unlist, not server-side deprecate
(answer 14).** The maintainer asked to manage package retirement "in one place" via GitHub Actions.
Verified against MS Learn (2025-10-31): nuget.org **deprecation** has **no** CLI/API (web-UI only;
there is no `dotnet nuget deprecate`), and `dotnet nuget delete` = **unlist** is strictly per-version.
Decision (**unlist-only**): a gated `unlist-deprecated` job in `publishPackages.yml` (`workflow_dispatch`
+ boolean input, `NUGET_API_KEY`) unlists **every** published version of `ApiClient` / `Story` /
`Scheduler` / `Guards`, with versions enumerated live from the flat-container index (never hardcoded).
Server-side deprecation is dropped to an optional, non-blocking web-UI follow-up; the successor mapping
lives in the doc-level migration map + `[Obsolete]` (Scheduler/Guards). This **corrected** the reviewed
plan's reference to a nonexistent `dotnet nuget deprecate`. Folded into the spec (decision 4 + Amendments),
step `00` (new answer 14), step `01` (the unlist job), step `07` (retirement = CI unlist), and step `10`
(runbook command fix). No code-step count change — it extends step `01`.
**Premortem gate EXECUTED 2026-06-30 — verdict: Go with mitigations.** The `premortem` skill was run
over the full plan (build-and-nuget, di, story checklists + failure modes 1–8 + answer 14). Ten
scenarios were worked backward; no `H`-severity scenario lacks a mitigation. Required mitigations
(each owned by a step): step 01 merges before 03–06 (publish gate); Logging `1.2.0` override (08);
repo-wide symbol-string sweep (03–07); `Authentication.Tests` in `.slnx` (06); unlist job gated on
`workflow_dispatch`+bool with scoped `NUGET_API_KEY` + a "drain in-flight Tale workflows before
migrating" note in the migration guide (01/10); slnx-membership guard (01). Accepted: unlisting breaks
floating-range `Story`/`ApiClient` consumers (intended, documented). Steps `01–11` (incl. `05b`) may
now proceed in order, each gated on the mitigation it owns. Full verdict + scenario table in
[`reviewed/00-run-premortem.md`](reviewed/00-run-premortem.md).

**Post-premortem follow-through 2026-06-30.** Two required mitigations the verdict named were **folded
back into their owning steps** (they had been asserted in the verdict but not yet written into the
steps): the **drain-before-migrate** warning for persisted workflows → step `10` (scenario 7), and the
**unlist-job operational guards** (hardcoded ids, misfire containment, web-UI-only relist recovery) →
step `01` (scenarios 6/10). The premortem also surfaced **four open questions** (`U1`–`U4`) recorded in
step `00` — **all answered by the maintainer 2026-06-30**: **U1 → ignore (pre-1.0, no persisted-state
compat)** so the drain note is **dropped** from step 10 and scenario 7 becomes an accepted risk;
**U2 → no** GitHub Environment / approval gate on the unlist job; **U3 → `NUGET_API_KEY` is full-account**
(gate + hardcoded ids are the sole containment); **U4 → unlist at `1.0`** (no grace period). None changed
the *Go with mitigations* verdict; all are propagated into steps `00/01/10`.











