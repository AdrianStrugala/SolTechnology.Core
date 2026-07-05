# ADR-013: Release 1.0 — versioning, hard registration-API rename, Story→Tale rebrand, deprecations, publish hardening

> **Status:** Proposed
> **Decision Date:** 2026-06-29
> **Decision Maker:** Adrian Strugała / Core maintainers

## Context

`SolTechnology.Core` ships ~20 supported NuGet packages plus 2 deprecated ones, all still on
`0.x` (except `SolTechnology.Core.Logging`, which already reached `1.1.1` — see below). The repo is
ready to commit to a stable `1.0` surface, but several cross-cutting problems block a clean cut:

1. **Inconsistent registration APIs.** Every module exposes a `ModuleInstaller` with `Add*` / `Use*`
   extensions, but the names are unbranded (`AddCQRS`, `AddSQL`, `UseCoreLogging`,
   `RegisterStories`, `AddAuthenticationAndBuildFilter`) and collide with first-party
   `Microsoft.Extensions.*` extension names in IntelliSense. A `1.0` is the only honest place to make
   the intentionally breaking rename to a uniform `AddSol*` / `UseSol*` convention.
2. **Version drift.** `<Version>` is hand-maintained per `.csproj`. Current values:
   `Logging 1.1.1`, `CQRS 0.8.0`, `Story 0.8.0`, `API 0.7.0`, `API.Testing 0.7.0`, `HTTP 0.7.0`,
   `SQL 0.6.0`, `MessageBus 0.6.0`, `Cache/BlobStorage/Authentication/AUID/Scheduler/Guards 0.5.0`,
   `Core/Hangfire/Testing/SQL.Testing/Redis.Testing/BlobStorage.Testing/ServiceBus.Testing 0.1.0`.
3. **Publish pipeline gaps.** `.github/workflows/publishPackages.yml` packs **14 of 22** packable
   projects. It **omits** `SolTechnology.Core.Hangfire` (a production library in the README) and **all
   7** `.Testing` companions (also in the README), and it **still packs** the deprecated `Scheduler`
   and `Guards`. The publish step (`dotnet nuget push`) runs on **every** push to `master`.
4. **`SolTechnology.Core.ApiClient` is a ghost.** It has **no `.csproj` and no source** (only stale
   `bin`/`obj` targeting `net6.0`/`net8.0`), yet it occupies the README "HTTP Clients" row, has
   `docs/Clients.md`, and is the **3rd most-downloaded** package in `nuget-stats.json` (v0.5.0,
   3 646 downloads). `SolTechnology.Core.HTTP` is its de-facto successor but has **no README row of
   its own**.
5. **NuGet metadata is thin.** `src/Directory.Build.props` centralises `Authors` + `RepositoryUrl`
   only; `Company`, `PackageProjectUrl`, license, `PackageReadmeFile`, and SourceLink are absent.
6. **Doc rot.** `docs/CICD.md` is empty; `SolTechnology.Core.slnx` references two non-existent files
   (`docs\future-ideas.md`, `docs\production-harvest-second-app.md`); deprecated/ghost topics still
   have docs (`Cron.md`, `Guards.md`, `Flow.md`, `Clients.md`).
7. **Two nouns for one framework.** The `SolTechnology.Core.Story` package mixes a *Story* noun
   (`StoryHandler`, `StoryManager`, `StoryController`, `RegisterStories`, `IStoryRepository`,
   namespace `SolTechnology.Core.Story`) with a *Tale* noun (`Tale<>`, `Tell()`, `TaleStep`, the
   README hero `CalculateBestPathTale`, and the "Tale Code" brand itself). The authoring surface a
   consumer writes (`Tell()` returning a `Tale`) is branded *Tale*, but the base class they inherit is
   `StoryHandler` and the package is `.Story`. `docs/Tale.md` is empty while `docs/Story.md` carries
   the content. `1.0` is the place to collapse this to a single brand noun.

This ADR records the locked `1.0` decisions, the breaking surface, and the sequencing that prevents
a half-finished rename from auto-publishing broken `0.x` patches. It supersedes the per-package
versioning practice and the ad-hoc registration naming.

> **Inventory note (verified against `src/**` on 2026-06-29).** The hand-gathered inventory that
> seeded this ADR had three material errors, corrected here and used as the source of truth:
> (a) `SolTechnology.Core.Logging` **already exposes** `AddCoreLogging` (2 overloads),
> `AddCorrelationIdService`, `AddLogScopeEnricher<T>`, and `LogDetail` — it is **not** "Add-less";
> (b) `SolTechnology.Core.Cache` exposes **six** `Add*` methods (`AddLocalCache`,
> `AddDistributedCache`, `AddLocalLock`, `AddDistributedLock`, `AddLocalIdempotency`,
> `AddDistributedIdempotency`) plus `AddRedisHealthCheck`, not two; (c) the rename surface also
> includes `SQL.AddSqlHealthCheck`, `Api.AddApiCoreFilters` (`MvcOptions`),
> `Api.UseSecurityHeaders`, `Api.MapCoreHealthChecks`, and `Hangfire.UseSolTechnologyFilters`
> (`IGlobalConfiguration`), none of which were in the original list.

## Decision

Ship a single coordinated `1.0` across all supported packages, executed in a strict order that keeps
`dotnet build SolTechnology.Core.slnx` green at every step and publishes **nothing** until a
deliberate release trigger fires.

1. **Versioning.** Every **supported** library → `1.0.0`. **Exception:**
   `SolTechnology.Core.Logging` → `1.2.0` (it accidentally shipped `1.1.1`, so `1.0.0` would be a
   downgrade NuGet rejects). Deprecated libraries (`Scheduler`, `Guards`, `ApiClient`) are **not**
   bumped to `1.0`. Mechanism: a shared `<Version>1.0.0</Version>` default in
   `src/Directory.Build.props`, with a per-project **override** in `Logging` (`1.2.0`) and explicit
   pins keeping `Scheduler`/`Guards` off the shared default.
2. **Hard rename, no `[Obsolete]` forwarders.** Every DI-registration extension `AddXxx` → `AddSolXxx`
   and every `IApplicationBuilder` middleware `UseXxx` → `UseSolXxx`. Type-forwarding is intentionally
   **not** provided — the break is what justifies `1.0.0` and a clean IntelliSense surface. All call
   sites in `sample-tale-code-apps/DreamTravel/**` and `tests/**` are updated in the **same PR** as
   each rename so the solution never goes red.
3. **`SolTechnology.Core.HTTP` is the official successor of `ApiClient`.** README "HTTP Clients" row
   repoints to `HTTP`; `ApiClient` is **unlisted** on nuget.org (every version, CI-automated — step 01)
   with a README / migration note naming `HTTP` as successor. No source is resurrected.
4. **Retirement mechanism — unlist (automated), not server-side deprecate.** nuget.org server-side
   *deprecation* (the "deprecated" badge + successor message) has **no** public CLI/API — it is
   web-UI-only (Manage packages → Deprecation; verified MS Learn 2025-10-31), so it is **dropped**
   from the release-blocking path. The repo-automatable action is `dotnet nuget delete` = **unlist**,
   run by a dedicated manual-only `Unlist deprecated packages` workflow
   (`.github/workflows/unlistDeprecatedPackages.yml`, step 01) that unlists **every** published version of
   each ghost id (`ApiClient`, `Story`, `Scheduler`, `Guards`) — versions enumerated live from the
   flat-container index, never hardcoded. Compile-time deprecation stays via `[Obsolete]` where source
   exists (`Scheduler`, `Guards`); the successor mapping is carried by the doc-level migration map
   (`dontreadme.md` + `docs/MIGRATION-0.x-to-1.0.md`). An optional manual web-UI deprecation is a
   non-blocking follow-up.
5. **Publish hardening.** Gate `dotnet nuget push` behind a deliberate release trigger (tag or
   `workflow_dispatch`) instead of every `master` push, and replace the 14 hand-listed `dotnet pack`
   steps with a glob/loop over every packable project (`IsPackable` / `PackageId`). This
   structurally fixes the "added a module, forgot CI" class of bug (auto-includes `Hangfire` + the 7
   `.Testing` companions) and prevents the incremental rename PRs from auto-publishing broken `0.x`.
6. **Metadata centralisation.** Add shared `Company`, `PackageProjectUrl`,
   `PackageLicenseExpression`, `PackageReadmeFile`, and SourceLink to `src/Directory.Build.props`.
7. **`Story` → `Tale` rebrand + package rename (accepted 2026-06-30).** Adopt a single **Tale** noun
   for the authoring layer. `SolTechnology.Core.Story` → **`SolTechnology.Core.Tale`** (new package
   id; folder, `.csproj`, `.slnx`, pack glob, README row all change); `StoryHandler<,,>` →
   **`TaleHandler<,,>`**; the subclass-naming convention `…Story` → **`…Tale`**; `RegisterStories` →
   **`AddSolTale`**. Per **option B2**, the rebrand is **full**: every public `Story*` infrastructure
   type (`StoryManager`, `StoryController`, `IStoryRepository`, `StoryOptions`,
   `StoryPausedError`/`StoryCancelledError`, `IStoryBuilder`, `StoryHandlerRegistry`,
   `StoryInstance`/`StoryStatus`, …) → `Tale*`, the root namespace becomes `SolTechnology.Core.Tale`,
   and the `…Story.Tale` sub-namespace **collapses** into the root (no `Tale.Tale`). The `Tale<>` /
   `Tell()` / `TaleStep` brand types are unchanged (already Tale). A `.Tale` package containing
   `StoryManager`/`StoryController` would reintroduce the very two-noun confusion this kills, so B2 is
   the only end-state coherent with the package rename. The old `SolTechnology.Core.Story` (0.8.0,
   310 downloads) becomes a **ghost** — handled exactly like `ApiClient` (decision 3/4): deprecated +
   unlisted on nuget.org with `SolTechnology.Core.Tale` named as successor, plus a migration note and
   a `dontreadme.md` row. As a brand-new package id, `Tale` ships its **first** version as a clean
   `1.0.0` (no downgrade concern). Executed as new **step 05b** (the single largest rename in the
   plan, kept in one build-green wave across `src` + tests + the DreamTravel sample), with the prose
   pass (guide, `CLAUDE.md`, the `command-query-event-story` skill, `docs/Story.md → Tale.md`,
   diagrams) owned by step 11.

## Alternatives Considered

1. **`[Obsolete]` forwarders instead of a hard rename.**
   *Pros:* non-breaking; consumers migrate at leisure.
   *Cons:* every old name lingers in IntelliSense beside its `Sol`-prefixed twin — the exact
   noise the rename removes; doubles the public surface; `1.0` would not actually be a clean cut.
   **Rejected** — the break is the point, and `1.0.0` is the sanctioned place for it.

2. **Bump straight to `1.0.0` with no rename (versioning only).**
   *Pros:* zero consumer churn.
   *Cons:* freezes the unbranded, collision-prone names forever; a later rename would need `2.0.0`.
   **Rejected.**

3. **Per-project `<Version>` kept as-is (no central default).**
   *Pros:* no override gymnastics for `Logging`.
   *Cons:* the drift that produced the accidental `Logging 1.1.1` recurs.
   **Rejected** in favour of a shared default + explicit `Logging`/`Scheduler`/`Guards` overrides.

4. **SourceLink via explicit `Microsoft.SourceLink.GitHub` PackageReference.**
   *Pros:* matches pre-`net8` muscle memory.
   *Cons:* on `net8+` (this repo is `net10.0`) SourceLink ships **in the SDK**; the explicit package
   is redundant. **Recommended: built-in SDK SourceLink** (`PublishRepositoryUrl`,
   `EmbedUntrackedSources`, `ContinuousIntegrationBuild`). Final approach + any version pin to be
   confirmed via the [`package-management`](../../.github/skills/package-management/SKILL.md) skill
   at implementation — **not guessed** (no SourceLink row exists in `canonical-versions.md` today).

5. **Keep packing deprecated `Scheduler`/`Guards` indefinitely vs one final deprecated publish vs
   stop packing now.** Surfaced as an open question for the premortem gate; default recommendation:
   one final deprecated publish carrying the `[Obsolete]` + banner, then drop from the pack glob.

6. **Story → Tale: merge to one noun, which one, and how deep.**
   - *Merge everything to `Story` (drop `Tale`).* Rejected — `Tell()` returning a `Story` and a
     `StoryEngine` running a `story` overloads one word three ways; `Tale` is the authoring brand
     ("Tale Code").
   - *Keep `Tale` only for the plan, keep the package/base class as `Story` (status quo).* Rejected —
     leaves the README hero (`…Tale`) contradicting every doc/example (`…Story`).
   - *Rename to `.Tale` but keep infra types as `Story*` (**option B1**).* Rejected — a `.Tale`
     package full of `StoryManager`/`StoryController` relocates the two-noun confusion rather than
     removing it.
   - **Full rebrand to `Tale*` + namespace collapse (option B2). Chosen** (decision 7) — the only
     end-state with a single noun. Cost: the widest rename in the release and a new package identity
     (old `.Story` deprecated like `ApiClient`).

## Consequences

**Positive**
- One uniform `AddSol*` / `UseSol*` surface; no more collisions with `Microsoft.Extensions.*`.
- `Hangfire` + 7 `.Testing` companions actually reach nuget.org; CI can no longer silently skip a
  new module.
- Version drift structurally prevented; the `Logging` downgrade trap is encoded as an override.
- `ApiClient` ghost resolved; consumers get a documented migration path to `HTTP`.

**Negative**
- Every consumer of every Core package takes a compile break on upgrade (mitigated by the migration
  guide + symbol-mapping table).
- Larger rename PRs (grouped by module cluster) than a typical change; each is mechanical but wide.
  The `Story → Tale` rebrand (step 05b) is the widest: package id, namespace, and ~15 public types
  move in one wave, and `using SolTechnology.Core.Story*` breaks with no type-forwarding.
- A second package identity to steward: `SolTechnology.Core.Story` joins `ApiClient`/`Scheduler`/
  `Guards` on the nuget.org deprecate-+-unlist runbook; consumers stay on the old `0.8.0` until they
  migrate to `SolTechnology.Core.Tale`.
- A window exists where `master` carries renamed APIs while nuget.org still serves `0.x` — acceptable
  because publishing is gated behind a deliberate release trigger and `--skip-duplicate`.

**Semver impact:** **MAJOR** — coordinated `1.0.0` (Logging `1.2.0`) with a breaking public-API
rename across all supported packages. `SolTechnology.Core.Tale` is a **new** package id whose first
publish is a clean `1.0.0`; `SolTechnology.Core.Story` is retired (**unlisted** on nuget.org).

## Amendments

- **2026-06-30 — retirement mechanism corrected (decision 4).** The original decision 4 specified
  nuget.org *server-side deprecation* via a manual runbook (`dotnet nuget deprecate`). That command
  **does not exist** — nuget.org deprecation is exposed only through the web UI (verified MS Learn
  2025-10-31). The repo-automatable action is `dotnet nuget delete` = **unlist** (per-version). Per
  the maintainer decision ("manage packages in one place", unlist-only), ghost-package retirement is
  now an **automated, manual-only `Unlist deprecated packages` workflow** (`unlistDeprecatedPackages.yml`,
  step 01) that unlists every published
  version of `ApiClient` / `Story` / `Scheduler` / `Guards`; server-side deprecation is dropped to an
  optional non-blocking follow-up. Recorded as step-00 answer 14; propagated into steps 01/07/10.
  Prose elsewhere in this ADR that reads "deprecated + unlisted" refers to the `[Obsolete]` /
  doc-level deprecation plus this unlist — not a nuget.org deprecation badge.

## Related

- [ADR-003](003-api-versioning-strategy.md) — API versioning (header `X-API-VERSION`), touched by the
  `AddVersioning` → `AddSolVersioning` rename.
- [ADR-005](005-http-production-defaults.md) — `SolTechnology.Core.HTTP`, the `ApiClient` successor.
- [ADR-007](007-cqrs-production-hardening.md) — `AddCQRS`, renamed here.
- [ADR-008](008-testing-framework-companions.md) — the 7 `.Testing` companions whose publish gap this
  ADR closes; also the `Faker` → `HTTP.Testing` migration that the `1.0` migration guide cross-links.
- [ADR-009](009-hangfire-persistent-events-and-jobs.md) — `Hangfire` (missing from CI today) and the
  `Scheduler` deprecation.
- [ADR-002](002-Story-Framework-Implementation.md) / [ADR-011](011-story-sqlite-extraction.md) — the
  Story framework and its SQLite repository, rebranded to **Tale** here (step 05b). These historical
  records are left as-is; only the live code/docs are renamed.
- [`docs/ClaudeCodingGuide.md` §2](../ClaudeCodingGuide.md) — the mandatory `ModuleInstaller` pattern
  the rename must preserve.

## Implementation

Delivered as an 11-step plan (plus the split-out `05b`), gated by a premortem (`00`) that returned
**Go with mitigations**. See the **Implementation summary** below — the per-step working folder
(`013-release-1.0/`) was deleted per the [ADR-006](006-implementation-plan-workflow.md) §5
collapse-on-completion rule.

## Implementation summary

Completed 2026-07-01. All 11 steps shipped (step `05` was split into `05` + `05b` when the accepted
"Tale noun" decision made the `Story → Tale` rebrand the single largest rename). The per-step working
folder (`docs/adr/013-release-1.0/`) was deleted per the ADR-006 collapse-on-completion rule; this
section is the durable record.

| # | Step | Shipped |
|---|---|---|
| 00 | Premortem gate | **Go with mitigations** — open questions resolved (`13a`→B2, `13b`→rename skill, `13c`→`api/tale`, breaking accepted). |
| 01 | CI publish gate + pack-by-glob | `.github/workflows/publishPackages.yml` gates publish behind the release trigger, packs the 21 `src/` slnx projects by glob; deprecated-id unlisting split into a dedicated manual-only `.github/workflows/unlistDeprecatedPackages.yml` (typed `confirm == 'UNLIST'` gate, no push/tag/PR trigger); `checkout@v2`→`@v4`. |
| 02 | NuGet metadata + SourceLink | Centralised license/icon/readme/repository + SourceLink into `src/Directory.Build.props`; added `LICENSE` (MIT), `docs/API.Testing.md`, `docs/SQL.Testing.md`; `Core` keeps the root `README.md`. |
| 03 | Rename wave 1 — Logging | `AddCoreLogging`→`AddSolLogging` (+ `LogScopeEnricher`/`CorrelationIdService`), class → `ModuleInstaller`; Api/HTTP callers + `docs/Log.md` swept; Logging 43 + HTTP 79 tests green. |
| 04 | Rename wave 2 — data + transport | 18 public symbols renamed (`AddCQRS`→`AddSolCQRS`, `AddHTTPClient`→`AddSolHTTPClient`, MessageBus/Cache/SQL/Hangfire); all in-slnx + DreamTravel call sites updated; docs deferred to step 11. |
| 05 | Rename wave 3 — Api + Authentication | `AddVersioning`→`AddSolVersioning` + Authentication registration renames, mechanical only. |
| 05b | Tale rebrand + package rename | New package identity `SolTechnology.Core.Tale`, ~15 public types `Story*`→`Tale*`, namespace collapse, route `api/story`→`api/tale`, tests + DreamTravel sample; manager/controller verb methods intentionally keep the `Story` suffix. |
| 06 | Authentication anti-pattern fix | Removed the `BuildServiceProvider` call; added the `SolTechnology.Core.Authentication.Tests` host. |
| 07 | Deprecate Scheduler + Guards | `[Obsolete]` + `<IsPackable>false>` on the source-bearing deprecated libs; `Story` ghost handled via the step-10 runbook (unlist). |
| 08 | Version flip 1.0.0 | Shared version → `1.0.0` (Logging `1.2.0`; Tale ships its first at `1.0.0`, clean new id, no downgrade). |
| 09 | README + successors + parity | README rows for the `HTTP` (ApiClient successor) + `Tale` packages; owns `Clients.md`. |
| 10 | Runbook + dontreadme + CICD | `docs/release-runbook-1.0.md`, filled `docs/CICD.md`, `dontreadme.md` successor rows; migration guide + CHANGELOG dropped (pre-1.0). Unlist via `dotnet nuget delete` (no `deprecate` CLI). |
| 11 | Doc integrity sweep | `Story.md`→`Tale.md` rewrite, skill `command-query-event-story`→`command-query-event-tale`, guide/CLAUDE/theDesign/diagrams swept, deprecation banners on Guards/Flow, canonical-versions reconciled; build green, live docs free of dead links. |

### Preserved deviations

- **Manager/controller verb methods keep the `Story` suffix** (`TaleManager.StartStory/ResumeStory/CancelStory/GetStoryState`, `TaleOptions.TaleIdPrefix = "STR"`). The *types* are `Tale*`; these method names were intentionally not renamed in 05b. Tale docs mirror the real API, not a naïve global rename.
- **Deprecated docs are landing pages, not deletions.** `Cron.md` / `Guards.md` / `Flow.md` kept with deprecation banners (Guards→FluentValidation `AbstractValidator<T>`, Flow→Tale); only `Story.md` was deleted (superseded by `Tale.md`).
- **Docs were swept only in step 11.** Steps 03–05b renamed symbols in `src`/`tests`/sample and their XML-doc/comment/exception/log strings but deliberately left `docs/` untouched until the step-11 integrity pass.
- **Published historical ADRs left as-is.** ADR-002 / ADR-011 (Story framework + SQLite) and the "before" symbol names in this ADR are historical records, not renamed (CLAUDE.md §2).
- **Pre-1.0 scope cuts.** No `MIGRATION-0.x-to-1.0.md`, no `CHANGELOG.md`, no per-`src` READMEs (root/`docs/*.md` READMEs reused); server-side nuget.org *deprecation* dropped in favour of CI *unlist*.
- **Diagrams renamed to match code** (`story-*`→`tale-*`, `Story*`→`Tale*`) as a rename-to-match fix, not new `diagram`-agent authoring.







