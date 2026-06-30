---
adr: 013-release-1.0
step: 00 of 11
status: reviewed
---

<!-- Reviewed (2026-06-30): recorded the maintainer's answers to all 12 open questions inline;
     corrected Q2 to enumerate ALL FOUR health checks (MessageBus AddServiceBusHealthCheck and
     HTTP AddUpstreamHttpHealthCheck<TCheck> were missing — blocker B1); fixed the premortem-skill
     link depth (was ../../../, a reviewed/done step file is four levels deep). The premortem skill
     run + Go/No-Go verdict is still produced when this gate EXECUTES — recording answers does not
     execute the gate.
     2026-06-30 (Tale decision): recorded decision 13 (Story→Tale noun + package rename), added
     failure mode 8 (a–f), and open sub-questions 13a (B1/B2), 13b (skill rename), 13c (base route).
     The gate now also blocks new step 05b.
     2026-06-30 (sub-questions answered): the maintainer CONFIRMED 13a→B2 (full Tale* rebrand +
     namespace collapse), 13b→RENAME the skill (command-query-event-story → command-query-event-tale),
     13c→CHANGE the route, breaking accepted ([api/story]→[api/tale], sample controller + routes
     rename, the verified contract snapshot is regenerated on purpose). 13b/13c change scope and are
     propagated into steps 05b + 11; 13a needed no structural change (05b was authored on B2). Failure
     mode 8c is REVERSED for the controller/route (snapshot now churns by design). Recording these
     answers does NOT execute the gate — the Go/No-Go verdict is still produced when step 00 runs. -->

# Step 00: Run premortem (gate) + resolve open questions

## Summary
The gate for ADR-013. Authored last (it can only premortem a complete plan) but numbered `00` so the
"lowest `⬜ to-do` first" rule runs it before any code. Invoke the
[`premortem`](../../../../.github/skills/premortem/SKILL.md) skill, imagine `1.0` shipped and broke
production, work backward through the failure modes below, and record a verdict. **No step `01–11`
(incl. `05b`) may move to `done/` until this returns *Go* / *Go with mitigations*** and every open
question below has a recorded answer or gate verdict. The maintainer answers (recorded 2026-06-30) are
folded into steps `01–11`/`05b`; this gate still has to be **executed** (premortem skill run +
verdict) before any step ships.

## Failure scenarios to work through
1. **Consumer breakage from the hard rename.** Every Core consumer takes a compile break. Did we ship
   the migration guide + symbol table (step 10) *with* the release, not after? Is the break uniform
   (no half-renamed module)? Blast radius confirmed: `DreamTravel.Api/Program.cs` (~13 call sites),
   `DreamTravel.Worker` (`AddLocalCache`, `AddCQRS`, `AddPersistentEvents`, `AddRecurringJob<FetchTrafficJob>`),
   `DreamTravel.Sql`, `DreamTravel.GeolocationDataClients`, and the `Logging` / `HTTP` /
   `Story`→`Tale` (step 05b) / `SQL` / `CQRS` / `Hangfire` / `MessageBus` / `Cache` / `Api` test projects.
2. **Stale references survive the rename (silent rot or runtime test break).** Renaming a symbol does
   not touch the `<c>` / `<see cref>` XML-doc references, code comments, or `throw` / log strings that
   name it. One pair is load-bearing: `Hangfire/ModuleInstaller.cs:27` throws
   `"AddPersistentEvents() requires AddCQRS() to be called first."` and
   `Hangfire.Tests/ModuleInstallerTests.cs:25` asserts `.WithMessage("*AddCQRS()*")` — if the throw
   string is renamed but the assertion is not (or vice-versa) the test fails at runtime, not compile.
   Mitigation: steps 03–07 each carry a repo-wide symbol-string sweep (see those steps).
3. **CI publishes a wrong/duplicate version.** Before step 01 lands, `dotnet nuget push` runs on every
   `master` push. If a rename PR merged first, it could publish a broken `0.x` patch. Mitigation:
   step 01 (release-trigger gate) **must** merge before steps 03–06. Confirm `--skip-duplicate` +
   the `if:` guard close the hole.
4. **Accidental Logging downgrade.** `Logging` is `1.1.1` on nuget.org; an inherited shared `1.0.0`
   would be rejected/disastrous. Confirm the step-08 override pins `Logging 1.2.0` and a pack dry-run
   shows `SolTechnology.Core.Logging.1.2.0.nupkg`.
5. **Authentication `BuildServiceProvider` anti-pattern.** Resolved: **fix** in `1.0` (step 06 runs,
   no longer contingent). Confirm auth behaviour is preserved (DreamTravel component/E2E) and the new
   `SolTechnology.Core.Authentication.Tests` host (step 06) is wired into `.slnx` so CI builds + runs it.
6. **nuget.org retirement steps forgotten.** Unlisting every published version of `ApiClient` /
   `Story` / `Scheduler` / `Guards` is now **CI-automated** (step 01's gated `unlist-deprecated` job,
   needs `NUGET_API_KEY`). Server-side *deprecation* (badge + successor message) has **no** CLI/API
   (web-UI only) and is an **optional** follow-up. Confirm the step-01 job + step-10 runbook are the
   durable record and part of the release checklist, not tribal knowledge.
7. **Discovered risks.** Per-package `PackageReadmeFile` with no README file → pack failure (step 02,
   now per-package READMEs — answer 9); first-time publish of `Hangfire` + 7 `.Testing` at `0.1.0`
   before the `1.0.0` flip; the slnx-driven pack glob silently drops any packable project missing
   from `.slnx` (relocated "forgot CI" risk — step 01 adds a guard).
8. **`Story` → `Tale` package rename + rebrand (decision 13 — new step 05b).** Distinct failure modes
   the ordinary `AddSol*` rename does **not** carry:
   - **(a) New package identity = silent stickiness.** `SolTechnology.Core.Story` (0.8.0, 310
     downloads) and `SolTechnology.Core.Tale` are *different* packages on nuget.org. Consumers keep
     resolving the old 0.8.0 forever unless `Story` is **unlisted** (every version, CI-automated —
     step 01) with `Tale` documented as successor in the migration map (step 10, mirrors
     `ApiClient → HTTP`). Confirm the CI unlist job + `dontreadme.md` row exist and a migration note
     ships **with** the release.
   - **(b) Namespace migration breaks every `using`.** `using SolTechnology.Core.Story*` →
     `…Tale*` (root collapse of `.Story.Tale`). No type-forwarding is possible — confirm the
     migration table (step 10) maps every namespace + type, and that the break is uniform (no
     half-renamed assembly leaving `dotnet build SolTechnology.Core.slnx` red mid-wave — 05b moves
     package + types + all call sites in **one** PR).
   - **(c) Inheritance / implementation points in the sample.** `DreamTravelStoryController : StoryController`
     and `SQLiteStoryRepository : IStoryRepository` are consumer **extension** points, not just call
     sites — they break on the base-type/interface rename. Confirm 05b updates the base type, usings,
     and ctor param types. **REVERSED by 13c (CONFIRMED — breaking accepted):** the sample controller
     **and its routes now rename too**, and the verified contract snapshot is **regenerated on purpose**
     (consumer-facing routes are a *sample* — breaking is acceptable per the maintainer: "it's a
     sample, screw the URL, we can break"). Concretely: `DreamTravelStoryController → DreamTravelTaleController`,
     `[Route("api/dreamtravel/story")] → [Route("api/dreamtravel/tale")]`, base `[Route("api/story")] →
     [Route("api/tale")]`, and `SQLiteStoryRepository → SQLiteTaleRepository` (must implement the
     renamed `ITaleRepository`; its `SQLiteStoryRepositoryOptions` renames with it). This **deliberately
     churns** the single component snapshot
     `sample-tale-code-apps/DreamTravel/tests/Component/ContractTests.ContractTest_reviewChangesToTheApi.verified.txt`
     — the OpenAPI tag `DreamTravelStory → DreamTravelTale` (lines 45/74/98/114/131/750) and the route
     segment `…/dreamtravel/story/… → …/dreamtravel/tale/…` (lines 42/71/128). 05b regenerates that
     `.verified.txt` as a **named deliverable** and the contract test must be green after regen.
   - **(d) Stale runtime string keyed on the type name.** The Tale controller routes by
     `handlerType.Name`, so renaming `SampleOrderWorkflowStory → SampleOrderWorkflowTale` changes the
     URL key; `SampleOrderWorkflowTests.cs` hard-codes `…/SampleOrderWorkflowStory/start` and 404s at
     **runtime** (not compile) if the string is not updated. **Plus 13c:** the route segment itself
     moves `…/dreamtravel/story/… → …/dreamtravel/tale/…`, so **every** hard-coded URL string in
     `SampleOrderWorkflowTests.cs` (5 of them — the `/start` one *and* the four `{storyId}` ones) needs
     the `story`→`tale` swap. Same class as failure mode 2.
   - **(e) The governing docs the decision's blast-radius omitted.** The "Story" authoring vocabulary
     is defined in `docs/ClaudeCodingGuide.md` §0/§3/§4 (the **binding** spec — "Story Framework",
     `StoryHandler`, `RegisterStories`, `IStoryRepository`) and indexed in `CLAUDE.md` (skill row,
     §187 topic table, gotchas) and the `command-query-event-story` skill. The skill cites the guide
     with "**the guide wins — fix it in the same PR** (§19)"; updating the skill but not the guide is
     a self-contradiction. These must be reconciled **together** (step 11 owns the whole prose pass).
     **13b CONFIRMED → the skill is renamed `command-query-event-story → command-query-event-tale`**
     (folder + `SKILL.md` heading/`name:` + every cross-ref). Confirm step 11 covers guide + `CLAUDE.md`
     + the renamed skill + `docs/Story.md → Tale.md` + diagrams, and fixes the pre-existing
     `TellStory()` → the real `Tell()` drift (present in the guide §488/§784 **and** the skill). NOTE:
     the cited `CalculateBestPathStory` "drift" has **zero** repo hits — the example is already
     `CalculateBestPathTale` everywhere (README, guide, theDesign, diagrams, sample); step 11 only
     verifies it, there is nothing to rename.
   - **(f) Forgetting the skill.** Authoring agents read the authoring skill; **13b CONFIRMED — it is
     renamed `command-query-event-story → command-query-event-tale`**. If it still says `StoryHandler`
     / `RegisterStories` after the API ships as `TaleHandler` / `AddSolTale`, every new handler is
     generated against a dead API. Step 11 renames the folder + heading, sweeps its Story-laden
     vocabulary, and repoints every cross-ref.

## Open questions (answered by maintainer — recorded 2026-06-30)
1. **Naming transform for names containing `Core`.**
   **→ Answer:** Insert `Sol` after the leading `Add` / `Use` / `Map` verb; if `Core` *immediately*
   follows that verb, **replace** `Core` with `Sol` (never produce `AddSolCore…`); a `Core` elsewhere
   in the name stays. Examples: `AddCoreLogging → AddSolLogging`, `UseCoreLogging → UseSolLogging`,
   `MapCoreHealthChecks → MapSolHealthChecks`, `AddApiCore → AddSolApiCore`,
   `AddApiCoreFilters → AddSolApiCoreFilters`.
2. **Health-check builder + endpoint extensions.**
   **→ Answer (corrected — B1):** there are **four** health-check builder extensions, not two —
   prefix **all four**: `AddSqlHealthCheck → AddSolSqlHealthCheck` (SQL),
   `AddRedisHealthCheck → AddSolRedisHealthCheck` (Cache),
   `AddServiceBusHealthCheck → AddSolServiceBusHealthCheck` (**MessageBus**, was missing),
   `AddUpstreamHttpHealthCheck<TCheck> → AddSolUpstreamHttpHealthCheck<TCheck>` (**HTTP**, was missing);
   plus the endpoint mapper `MapCoreHealthChecks → MapSolHealthChecks`. (Entry points get `Sol`;
   `UseSecurityHeaders → UseSolSecurityHeaders` and `AddApiCoreFilters → AddSolApiCoreFilters` follow
   the same rule.)
3. **MessageBus `With*`** (`WithTopicPublisher`/`WithTopicReceiver`/`WithQueuePublisher`/
   `WithQueueReceiver`). **→ Answer:** keep `With*` **unprefixed** (fluent continuation, not an entry
   point).
4. **Story builder methods** (`UseInMemoryStoryRepository`, `UseStoryRepository<T>` on `IStoryBuilder`).
   **→ Answer:** keep **unprefixed** (builder-scoped continuations).
5. **`Hangfire.UseSolTechnologyFilters`** (`IGlobalConfiguration`).
   **→ Answer:** rename to `UseSolFilters` (entry-point in the Hangfire config chain; convention parity).
6. **`LogDetail`.** **→ Answer:** keep as-is (a fluent continuation, not an `Add*` entry point).
7. **Authentication anti-pattern.** **→ Answer:** **fix in `1.0`** — drop `BuildServiceProvider()`,
   return `IServiceCollection`. Step 06 runs (no longer contingent). This triggers the missing-test-host
   blocker, resolved in step 06 by creating `SolTechnology.Core.Authentication.Tests`.
8. **SourceLink.** **→ Answer:** built-in SDK SourceLink (`net10`); **no** `PackageReference`. Record
   the "no explicit SourceLink package" decision via the
   [`package-management`](../../../../.github/skills/package-management/SKILL.md) skill (no version
   pin to guess — it ships in the SDK).
9. **`PackageReadmeFile`.** **→ Answer (overrides the earlier shared-root recommendation):** **each
   package gets its own README** (one per shipped package), referenced via `PackageReadmeFile` per
   project. The per-module `docs/*.md` are the natural source content. This is more authoring work
   than a single shared README — step 02 scope + acceptance reflect it.
10. **Deprecated packs (`Scheduler`/`Guards`).** **→ Answer:** **stop packing now.** Both are already
    outside `.slnx`, so the slnx-driven pack glob (step 01) excludes them for free; `[Obsolete]` in
    source + nuget.org **unlist** (CI-automated, step 01) protect existing consumers. No "one final
    deprecated publish."
11. **`ApiClient` on nuget.org.** **→ Answer:** **unlist** every version (CI-automated, step 01 —
    `dotnet nuget delete`); successor `HTTP` documented in the migration map. Server-side *deprecate*
    has no CLI/API (web-UI only, optional). Repurpose `docs/Clients.md` as the `HTTP` doc (or a thin
    redirect), **owned by step 09**; step 11 only verifies link integrity.
12. **Release trigger.** **→ Answer:** git tag `v1.0.0` **and** `workflow_dispatch` (both).
13. **`Story` → `Tale` noun + package rename (ACCEPTED 2026-06-30).** **→ Decision:** adopt **Tale**
    for the authoring layer and rename the package. `SolTechnology.Core.Story` → **`SolTechnology.Core.Tale`**
    (new package id; folder, `.csproj`, `.slnx`, pack glob, README row, `PackageId` all change);
    `StoryHandler<,,>` → **`TaleHandler<,,>`**; subclass suffix `…Story` → **`…Tale`**
    (`SampleOrderWorkflowStory` → `…Tale`; the README hero `CalculateBestPathTale` already is Tale;
    `CityDomainService` **keeps** its DomainService name); `RegisterStories` → **`AddSolTale`**. The
    `Tale<>` / `Tale<TContext,TOutput>` / `Tell()` / `TaleStep` brand types **stay** (already Tale).
    The old `SolTechnology.Core.Story` (0.8.0, 310 downloads) becomes a **ghost** → **unlisted** on
    nuget.org (every version, CI-automated — step 01) with `SolTechnology.Core.Tale` documented as
    successor, a Story→Tale migration note, and a `dontreadme.md` row (mirrors `ApiClient → HTTP`).
    Folded into the plan as new **step 05b** (code),
    plus steps 01/07/08/09/10/11. **No `[Obsolete]` shims** (type names change wholesale; the old
    package keeps its old source) — handled purely by the CI unlist + the migration
    doc.
    - **Open sub-question 13a — infra `Story*` rebrand depth.** How far does the rename go for the
      public `Story*` types that are **not** the base class (`StoryManager`, `StoryController` +
      `StoryInstanceDto`/`StoryResultDto`, `StoryHandlerRegistry`, `StoryOptions` with
      `StoryIdPrefix`, `StoryPausedError`/`StoryCancelledError`, `IStoryRepository`/`InMemoryStoryRepository`,
      `IStoryBuilder`/`StoryBuilder`/`StoryBuilderExtensions` with `UseStoryRepository<T>`/`UseInMemoryStoryRepository`,
      `StoryInstance`/`StoryStatus`)?
      - **B2 (RECOMMENDED) — full rebrand to `Tale*`**, root namespace `SolTechnology.Core.Tale`,
        **collapse** the `…Story.Tale` sub-namespace into the root so `Tale<>` / `TaleStep` live in
        `SolTechnology.Core.Tale` (no ugly `SolTechnology.Core.Tale.Tale`). The only end-state
        coherent with a `.Tale` package — a `.Tale` package full of `StoryManager`/`StoryController`
        reintroduces the two-noun confusion this change kills.
      - **B1 (alternative) — keep infra `Story*`** inside the `.Tale` package/namespace. Smaller, but
        incoherent.
      - **→ ANSWERED → B2 (CONFIRMED 2026-06-30).** Full rebrand to `Tale*` + collapse the
        `…Story.Tale` sub-namespace into root `SolTechnology.Core.Tale`. Step 05b was authored on B2,
        so **no structural change** beyond what is already in 05b — this is a verdict-recording change.
    - **Open sub-question 13b (NEW) — does the rebrand rename the `command-query-event-story` skill?**
      The skill folder/name + heading literally end in "-story", and `CLAUDE.md` §3 / `docs/CQRS.md` /
      `docs/Story.md` / `docs/ClaudeCodingGuide.md` cross-link it. Options: (i) update its **vocabulary
      only** (keep the `command-query-event-story` name); (ii) **also rename** it to
      `command-query-event-tale` (folder + heading + every cross-ref).
      **→ ANSWERED → RENAME (CONFIRMED 2026-06-30).** Rename the skill `command-query-event-story →
      command-query-event-tale`: folder `.github/skills/command-query-event-story/` →
      `.github/skills/command-query-event-tale/`, the `SKILL.md` `name:`/heading, the Story-laden
      vocabulary inside it, and **every** cross-ref. Verified cross-ref surface (repo-wide search for
      `command-query-event-story`): `CLAUDE.md:88` (skill-index row), `docs/CQRS.md:162`,
      `docs/Story.md:545` + `:559` (→ `Tale.md`), `docs/ClaudeCodingGuide.md:174` + `:227`. Also fix
      the `TellStory() → Tell()` drift. Step 11 owns this and enumerates the hits.
    - **Open sub-question 13c (NEW) — base REST route.** `StoryController` declares
      `[Route("api/story")]` (an HTTP **contract**, inherited by consumers who do not override).
      Keep `api/story`, or change to `api/tale`?
      **→ ANSWERED → CHANGE THE ROUTE; BREAKING ACCEPTED (CONFIRMED 2026-06-30 — this INVERTS the
      prior "keep" recommendation).** Maintainer: *"it's a sample, screw the URL, we can break."*
      Base `[Route("api/story")] → [Route("api/tale")]`; sample `DreamTravelStoryController →
      DreamTravelTaleController` with `[Route("api/dreamtravel/story")] → [Route("api/dreamtravel/tale")]`;
      `SampleOrderWorkflowStory → SampleOrderWorkflowTale` flips the `handlerType.Name` URL key, so the
      start URL becomes `…/dreamtravel/tale/SampleOrderWorkflowTale/start`. The component snapshot
      `ContractTests.ContractTest_reviewChangesToTheApi.verified.txt` is **regenerated on purpose**
      (05b deliverable) and the contract test is green after regen. Folded into step 05b.
14. **Ghost-package retirement mechanism — unlist vs deprecate, manual vs CI (NEW — ASKED 2026-06-30).**
    Can the four ghost ids (`ApiClient`, `Story`, `Scheduler`, `Guards`) be retired on nuget.org from
    GitHub Actions, and should the release *deprecate*, *unlist*, or both?
    **→ ANSWERED → UNLIST-ONLY, CI-AUTOMATED (CONFIRMED 2026-06-30).** Verified against MS Learn
    (2025-10-31): nuget.org server-side **deprecation** (the "deprecated" badge + successor message)
    is exposed **only** through the web UI (Manage packages → Deprecation) — there is **no**
    `dotnet nuget deprecate` command and no public API. The repo-automatable action is
    `dotnet nuget delete` = **unlist**, which is strictly **per-version** (no "unlist whole package"
    command). Decision: a gated `unlist-deprecated` job in `publishPackages.yml` (step 01,
    `workflow_dispatch` + boolean input, `NUGET_API_KEY`) unlists **every** published version of each
    ghost id, with versions enumerated live from the flat-container index
    (`https://api.nuget.org/v3-flatcontainer/{id}/index.json` — never hardcoded; `nuget-stats.json`
    only records the latest). **Server-side deprecation is dropped** from the release-blocking path;
    the successor mapping (`Story→Tale`, `ApiClient→HTTP`, `Scheduler→Hangfire`,
    `Guards→FluentValidation`) is carried by the doc-level migration map + `[Obsolete]`
    (Scheduler/Guards only). An **optional** manual web-UI deprecation is recorded as a non-blocking
    follow-up in the step-10 runbook. This corrects the earlier (reviewed) plan text that referenced a
    nonexistent `dotnet nuget deprecate`. Propagated into steps 01/07/10.

## Acceptance criteria
- [x] `premortem` skill executed; failure modes 1–8 each have a cause + mitigation recorded in this
      file.
- [x] All 13 recorded decisions/answers are reflected into steps `01–11` (incl. new step `05b`);
      sub-questions `13a` (**→ B2**), `13b` (**→ rename skill**), and `13c` (**→ route to `api/tale`,
      breaking accepted, snapshot regenerated**) are answered and propagated (13b→step 11; 13c→step 05b;
      13a→no structural change). Answer `14` (**→ unlist-only, CI-automated; deprecate dropped**) is
      propagated into steps `01/07/10`. The Go/No-Go verdict below is still produced when the gate
      executes.
- [x] Verdict recorded: **Go** / **Go with mitigations** / **No-Go**. A *No-Go* names the step that
      must absorb the mitigation before the plan proceeds.
- [x] This step touches no `src/` code — it produces only this record.

## Verdict — executed 2026-06-30

**GO WITH MITIGATIONS.** No `H`-severity scenario lacks a plausible mitigation; every `H` is covered
by the plan's sequencing or an existing step. The change ships per-step only once the mitigation each
step owns is in place.

### Scenarios (worked backward from "1.0 shipped and broke prod")

| # | Scenario | Trigger | Blast | Sev | Lik | Existing control | Mitigation (owning step) |
|---|---|---|---|---|---|---|---|
| 1 | Hard rename breaks consumer builds | every `ModuleInstaller.cs`; `DreamTravel.Api/Program.cs` | public + sample | H | H | call sites updated same-PR | migration guide + symbol table ships **with** release (10); each wave build-green (03–06) |
| 2 | Stale symbol in `throw`/log/XML-doc → runtime test break | `Hangfire/ModuleInstaller.cs:27` ↔ `Hangfire.Tests/ModuleInstallerTests.cs:25` | internal | M | M | string-assert tests | repo-wide symbol-string sweep (03–07) |
| 3 | CI auto-publishes a broken `0.x` before the gate lands | `publishPackages.yml` push-on-master | public | H | M | none pre-01 | **step 01 merges before 03–06**; `--skip-duplicate` + `if:` guard |
| 4 | Logging `1.1.1`→`1.0.0` downgrade rejected | shared `1.0.0` in `src/Directory.Build.props` | public | H | M | none | step 08 override `Logging 1.2.0` + pack dry-run |
| 5 | `BuildServiceProvider` fix regresses auth | `Authentication/ModuleInstaller.cs` (06) | sample + public | M | M | new test host | `Authentication.Tests` wired into `.slnx` (06) + DreamTravel E2E |
| 6 | Unlist job (answer 14) fires on a normal publish / wrong id | `publishPackages.yml` `unlist-deprecated` | public | H | L | gated `workflow_dispatch`+bool | never on tag/master; 4 hardcoded ghost ids (full-account key, U3); no Environment gate (U2) (01) |
| 7 | Persisted `Story` state fails to deserialize after `Tale` migration | `Story→Tale` namespace collapse (05b) | public (migrators) | H | L | `StoryJsonOptions`; Tale = new opt-in id | **accepted (U1): pre-1.0, no persisted-state compat promised**; in-memory default unaffected (10) |
| 8 | Unlist breaks floating-range consumers of `Story`/`ApiClient` | `dotnet nuget delete` per-version (01) | public | M | M | intended | documented in `dontreadme` + migration map (10) — **accepted** |
| 9 | Pack glob silently drops a packable project missing from `.slnx` | slnx-driven glob (01) | public | M | L | none (relocated) | step 01 slnx-membership fail-fast guard |
| 10 | `NUGET_API_KEY` missing/mis-scoped → unlist job fails | repo secret | internal | L | M | none | loud job failure; key in release checklist (10) |

### Required mitigations before merge (per owning step)

1. **Sequencing** — step 01 (publish gate) merges **before** steps 03–06. (Scenario 3)
2. **Logging** — step 08 pins `Logging 1.2.0`; pack dry-run shows `SolTechnology.Core.Logging.1.2.0.nupkg`. (Scenario 4)
3. **Symbol-string sweep** — steps 03–07 sweep `throw`/log/XML-doc; the `Hangfire:27` ↔ test:25 pair is the load-bearing case. (Scenario 2)
4. **Auth test host** — `SolTechnology.Core.Authentication.Tests` wired into `.slnx`. (Scenario 5)
5. **Unlist job** — gated **only** on `workflow_dispatch` + boolean; the 4 ghost ids are hardcoded
   (full-account key per U3, so the gate + hardcoded ids are the sole containment; no Environment gate
   per U2). Persisted-state migration is **out of scope (U1, pre-1.0)** — no drain note. (Scenarios 6, 7)
6. **slnx guard** — step 01 fail-fast when a packable `src/` project is absent from `.slnx`. (Scenario 9)

### Accepted risks

- **Scenario 8** — unlisting breaks floating-range consumers of `Story`/`ApiClient`: intended, documented, successor named.
- **Scenario 7** — persisted `Story` workflow state not migrated to `Tale` types: **accepted (U1)** — the
  repo is pre-1.0; no persisted-state compatibility is promised. In-memory-repository consumers (default)
  are unaffected.
- The window where `master` carries renamed APIs while nuget.org still serves `0.x`: acceptable (gated publish + `--skip-duplicate`).

The gate is now **executed**. Steps `01–11` (incl. `05b`) may proceed in order, each gated on the
mitigation it owns above.

### Open questions surfaced by the premortem (need a maintainer call — not resolvable from code)

These do **not** block the *Go with mitigations* verdict (each has a safe default), but they are real
calls the maintainer should make before the unlist job runs and before `1.0` ships:

- **U1 — Do any external consumers persist workflow state?** Scenario 7's severity (`H`) assumes a
  durable `IStoryRepository`. We cannot see public consumers. **→ ANSWERED (2026-06-30): ignore — we are
  pre-1.0.** No persisted-state compatibility is promised; the drain-before-migrate note is **dropped**
  from step 10 and scenario 7 is an accepted risk. In-memory default is unaffected.
- **U2 — Should the `unlist-deprecated` job require manual approval?** A mis-fired unlist is silent (no
  build break) and the only undo is the web UI. **→ ANSWERED (2026-06-30): no.** No GitHub Environment /
  required reviewer; the `workflow_dispatch`+boolean gate + hardcoded ids are the agreed protection.
- **U3 — Is `NUGET_API_KEY` scoped?** **→ ANSWERED (2026-06-30): full-account key.** It is **not** scoped
  to the four ghost ids, so the gate + hardcoded ids are the sole containment (recorded in step 01).
- **U4 — Unlist timing: at `1.0` or after a grace period?** **→ ANSWERED (2026-06-30): at `1.0`.** The
  unlist runs as part of the release — no grace window; the migration map ships in the same release.

## Open questions
- **13a — ANSWERED → B2** (full `Tale*` rebrand + `…Story.Tale` namespace collapse into root
  `SolTechnology.Core.Tale`). 05b was authored on B2 → no structural change.
- **13b — ANSWERED → rename the skill** `command-query-event-story → command-query-event-tale`
  (folder + heading + every cross-ref; + `TellStory() → Tell()` drift fix). Owned by step 11.
- **13c — ANSWERED → change the route, breaking accepted.** `[Route("api/story")] → [Route("api/tale")]`,
  sample controller + routes rename, `SQLiteStoryRepository → SQLiteTaleRepository`, verified snapshot
  regenerated on purpose. Folded into step 05b.
- All three sub-questions are now resolved and propagated. **The only item still open is the gate's
  own execution** — the premortem skill run + Go / No-Go verdict, produced when step `00` runs.















