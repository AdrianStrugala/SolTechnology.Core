---
adr: 013-release-1.0
step: 10 of 11
status: reviewed
---

<!-- Reviewed (2026-06-30): folded the dontreadme.md stale-symbol fix into this step (M1 — its
     AddRecurringJob<TJob>(cron) row must become AddSolRecurringJob<TJob>(cron)); added a runbook check
     that each package's per-package README (step 02, answer 9) renders on nuget.org; confirmed the
     ApiClient deprecate + unlist runbook (answer 5).
     2026-06-30 (Tale decision): added the Story→Tale migration sub-table, the Story deprecate+unlist
     runbook entry (successor Tale) + Tale 1.0.0 publish check, and the dontreadme Story→Tale row.
     2026-06-30 (unlist decision — answer 1): corrected the runbook — there is NO `dotnet nuget
     deprecate` command (server-side deprecation is web-UI only; verified MS Learn 2025-10-31). The
     repo-automatable action is `dotnet nuget delete` = *unlist*, now run by the gated `unlist-deprecated`
     CI job (step 01) across every published version. nuget.org server-side *deprecation* is DROPPED;
     successor mapping lives in the doc-level migration map + `[Obsolete]` (Scheduler/Guards). -->

# Step 10: Migration guide + CHANGELOG + release runbook + `dontreadme` + fill `CICD.md`

## Summary
Author the human-facing release artifacts: a `0.x → 1.0` migration guide with the full old→new symbol
table, a CHANGELOG, a release runbook capturing the manual nuget.org actions that cannot be done from
the repo, the missing `ApiClient` deprecation row, and the contents of the empty `docs/CICD.md`.
Docs-only PR; depends on the final rename names (steps 03–06) and versioning (step 08) being settled.

## Affected components
- `CHANGELOG.md` — NEW — `1.0.0` entry (Logging `1.2.0`), breaking-rename summary, deprecations (incl. `Story → Tale`).
- `docs/MIGRATION-0.x-to-1.0.md` — NEW — symbol map + package moves (incl. the `Story → Tale` sub-table).
- `docs/release-runbook-1.0.md` — NEW — release checklist (CI-automated unlist via step 01 + the web-UI-only deprecation note).
- `dontreadme.md` — EDIT — add the `ApiClient → HTTP` **and** `Story → Tale` rows; fix the stale `AddRecurringJob` symbol.
- `docs/CICD.md` — EDIT — fill (publish pipeline, versioning policy, unlist/deprecation runbook).

## Changes
- **Migration guide** — a table of every renamed symbol `old → new` (all `AddSol*` / `UseSol*` from
  steps 03–06, including the four `AddSol*HealthCheck` names, `MapSolHealthChecks`,
  `UseSolSecurityHeaders`, `AddSolApiCoreFilters`, `UseSolFilters`, `AddSolTale`) + package moves:
  `ApiClient → HTTP`, `Story → Tale`, `Scheduler → Hangfire` (`AddSolRecurringJob<T>`),
  `Guards → FluentValidation`, and a cross-link to ADR-008's `Faker → HTTP.Testing`.
- **`Story → Tale` migration sub-table (decision 13 / step 05b — B2).** A dedicated table because it
  is a package + namespace + type rename, not just a registration verb:
  | Old | New |
  |---|---|
  | package `SolTechnology.Core.Story` | `SolTechnology.Core.Tale` |
  | namespace `SolTechnology.Core.Story[.Builder/.Orchestration/.Persistence/.Api/.Models]` | `SolTechnology.Core.Tale[.Builder/.Orchestration/.Persistence/.Api/.Models]` |
  | namespace `SolTechnology.Core.Story.Tale` | `SolTechnology.Core.Tale` (collapsed root) |
  | `StoryHandler<,,>` | `TaleHandler<,,>` |
  | `RegisterStories(...)` | `AddSolTale(...)` |
  | `StoryManager` | `TaleManager` |
  | `StoryController` / `StoryInstanceDto` / `StoryResultDto` | `TaleController` / `TaleInstanceDto` / `TaleResultDto` |
  | `StoryHandlerRegistry` | `TaleHandlerRegistry` |
  | `StoryOptions` (`StoryIdPrefix`) | `TaleOptions` (`TaleIdPrefix`) |
  | `StoryPausedError` / `StoryCancelledError` | `TalePausedError` / `TaleCancelledError` |
  | `IStoryRepository` / `InMemoryStoryRepository` | `ITaleRepository` / `InMemoryTaleRepository` |
  | `IStoryBuilder` / `StoryBuilder` / `UseStoryRepository<T>` / `UseInMemoryStoryRepository` | `ITaleBuilder` / `TaleBuilder` / `UseTaleRepository<T>` / `UseInMemoryTaleRepository` |
  | `StoryInstance` / `StoryStatus` | `TaleInstance` / `TaleStatus` |
  | subclass suffix `…Story` (e.g. `SampleOrderWorkflowStory`) | `…Tale` (`SampleOrderWorkflowTale`) |
  | `Tale<>` / `Tale<,>` / `Tell()` / `TaleStep` | **unchanged** (already the Tale brand) |

  (Under **B1** this sub-table shrinks to the base class + package + namespace rows; the gate's `13a`
  verdict decides. `CityDomainService` keeps its name; `Tell()` is the real method — the docs’ old
  `TellStory()` is a drift fixed in step 11.)
- **CHANGELOG** — `## [1.0.0]` with Added / Changed (breaking renames) / Deprecated / Removed; note
  `Logging 1.2.0` and why it differs.
- **Release runbook** — ordered release steps: tag `v1.0.0` (triggers the gated publish from step 01)
  or run `workflow_dispatch`; **unlist the four ghost ids is CI-automated** — run the gated
  `unlist-deprecated` job (step 01, `workflow_dispatch` + boolean input) which unlists **every**
  published version of `ApiClient`, `Story`, `Scheduler`, `Guards` via `dotnet nuget delete` (needs
  `NUGET_API_KEY` as a repo secret — answers 5/10/13). **Server-side *deprecation* (the "deprecated"
  badge + successor message) is NOT automatable** — nuget.org exposes it only through the web UI
  (Manage packages → Deprecation); there is no `dotnet nuget deprecate` command or public API
  (verified MS Learn 2025-10-31). Per **answer 14** the release does **unlist-only** via CI; the
  optional manual web-UI deprecation (to set `Tale`/`HTTP`/`Hangfire`/`FluentValidation` as the named
  successor) is recorded as an **optional** follow-up, not a release blocker. Then verify Logging
  published as `1.2.0` not `1.0.0`; verify `SolTechnology.Core.Tale` published as `1.0.0` (new id —
  first version); confirm `Hangfire` + 7 `.Testing` companions appear on nuget.org; spot-check that
  each package's per-package README (step 02) renders on its nuget.org page (for `Tale`, the README
  is `docs/Tale.md` content authored in step 11).
- **`dontreadme.md`** — (1) **fix the stale symbol** in the existing `Scheduler → Hangfire` row:
  `AddRecurringJob<TJob>(cron)` → `AddSolRecurringJob<TJob>(cron)` (M1); (2) add a new row:
  `SolTechnology.Core.ApiClient | SolTechnology.Core.HTTP | No source remained (ghost); unlisted on
  nuget.org (CI). Use AddSolHTTPClient<…>. See docs/HTTP*.`; (3) add a `Story → Tale` row
  (decision 13): `SolTechnology.Core.Story | SolTechnology.Core.Tale | Renamed (Tale brand);
  unlisted on nuget.org (CI). StoryHandler→TaleHandler, RegisterStories→AddSolTale, namespace
  SolTechnology.Core.Tale. See docs/Tale.md.`
- **`docs/CICD.md`** — document the glob-pack + release-trigger pipeline (step 01, incl. the
  slnx-membership guard **and** the gated `unlist-deprecated` job), the shared-version +
  Logging-override policy (step 08), the unlist-vs-deprecate boundary (CI unlists; deprecation is
  web-UI-only and optional), and the note that `nuget-stats.json` is a generated snapshot.

## Acceptance criteria
- [ ] `docs/MIGRATION-0.x-to-1.0.md` lists every renamed public symbol with old + new name (all four
      health checks and the endpoint/middleware names included) **and** the `Story → Tale` sub-table
      (package + namespace + types).
- [ ] `CHANGELOG.md` has a `1.0.0` entry that names the Logging `1.2.0` exception and the `Story → Tale`
      package rename.
- [ ] `docs/release-runbook-1.0.md` enumerates the tag / `workflow_dispatch` trigger + the
      CI-automated `unlist-deprecated` step (incl. `Story`, `ApiClient`, `Scheduler`, `Guards`) with
      the `NUGET_API_KEY` prerequisite called out, states that server-side **deprecation is web-UI-only
      and optional** (no `dotnet nuget deprecate` exists), the `Tale 1.0.0` publish verification, and
      the per-package README spot-check.
- [ ] `dontreadme.md` has `ApiClient` **and** `Story` rows and no stale `AddRecurringJob` (now `AddSolRecurringJob`).
- [ ] `docs/CICD.md` is no longer empty and covers pipeline + versioning + unlist (CI) vs deprecation (web UI).

## Open questions
- none — content is determined by the decisions resolved at step 00 and applied in steps 01–09.







