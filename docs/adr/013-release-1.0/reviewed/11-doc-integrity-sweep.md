---
adr: 013-release-1.0
step: 11 of 11
status: reviewed
---

<!-- Reviewed (2026-06-30): fixed the documentation-cleanup skill link depth (was ../../../, a
     reviewed/done step file is four levels deep); made step 11 verify-only for the Clients.md → HTTP
     disposition (step 09 owns it — resolves the 09↔11 circular dependency); recorded recommended
     dispositions for Cron.md / Guards.md / Flow.md aligned with the deprecation answers, flagged as
     the documentation-cleanup skill's call since the maintainer did not pin them at step 00.
     2026-06-30 (Tale decision): this step now OWNS the whole Story→Tale prose pass — docs/Story.md→Tale.md
     (+ the .Tale.csproj readme flip), ClaudeCodingGuide §0/§3/§4, CLAUDE.md, the command-query-event-story
     skill (+ TellStory()→Tell() drift), and diagrams. The
     skill cites the guide (§19 "guide wins, same PR"), so skill+guide are reconciled together here;
     prose does not affect build-greenness, so it lands safely after the step-05b code wave.
     2026-06-30 (13b answered): the skill is DEFINITIVELY renamed `command-query-event-story →
     command-query-event-tale` (folder + heading + `name:` + every cross-ref) — no longer "whichever is
     chosen". The `CalculateBestPathStory` "drift" has zero repo hits (already `CalculateBestPathTale`
     everywhere) → verify only. Added targets: `docs/theDesign.md` (Story prose) and the skill's
     `SaveCityStory → SaveCityTale` reference path (05b renamed that namespace). -->

# Step 11: Documentation integrity sweep (route execution to the `documentation-cleanup` skill)

## Summary
Close the remaining doc-rot items as a final pass, driven by the
[`documentation-cleanup`](../../../../.github/skills/documentation-cleanup/SKILL.md) skill. This step
enumerates the concrete targets; the skill does the validation + fixes (module↔doc parity, links,
tables, Mermaid, ADR formatting). Docs-only PR, last so it can also verify the docs written in steps
09–10 — including that the `HTTP` doc / `Clients.md` disposition **owned by step 09** resolves cleanly.

## Affected components
- `SolTechnology.Core.slnx` — EDIT — remove/fix the two dangling `<File>` refs. (Note: `docs/Story.md`
  is **not** referenced in the `.slnx` Docs folder, so the doc rename needs no `.slnx` Docs edit; the
  two project rows are owned by step 05b.)
- `docs/Story.md` → `docs/Tale.md` — **MOVE + REWRITE (decision 13, B2)** — this step **owns** the
  562-line content move: every example `StoryHandler`/`RegisterStories`/`StoryController`/`IStoryRepository`/`StoryOptions`
  → `TaleHandler`/`AddSolTale`/`TaleController`/`ITaleRepository`/`TaleOptions`, headings
  "Story Framework" → "Tale Framework". Dispose of `docs/Story.md` (delete, or leave a one-line
  redirect to `docs/Tale.md`). `docs/Tale.md` already exists (empty).
- `src/SolTechnology.Core.Tale/SolTechnology.Core.Tale.csproj` — EDIT — flip the readme packaging
  `<None Include="..\..\docs\Story.md" … PackagePath="docs\readme.md" />` → `docs\Tale.md` **in the
  same step as the content move**, so the doc file and its packaging pointer move together (no
  empty-readme pack window; step 05b deliberately left this at `Story.md`).
- `docs/ClaudeCodingGuide.md` §0/§3/§4 — EDIT — the **binding** Story vocabulary the decision's
  blast-radius omitted: `StoryHandler` → `TaleHandler`, `RegisterStories` → `AddSolTale`,
  `IStoryRepository`/`UseStoryRepository<T>` → `ITaleRepository`/`UseTaleRepository<T>`,
  "Story Framework" (§4 header) → "Tale Framework", "a Story" → "a Tale", the `…Story.cs` filename
  note. Concrete hits: `typeof(SaveCityStory)` examples (`:415`, `:419`) → `typeof(SaveCityTale)` (05b
  renamed that namespace); `TellStory()` drift (`:488`, `:784`) → `Tell()`; the `← StoryHandler
  implementation` comment (`:135`) → `TaleHandler`. (The guide already uses `CalculateBestPathTale.cs`
  as the filename — only the base-type, registration, and `SaveCityStory`/`TellStory()` refs are stale.)
- `CLAUDE.md` — EDIT — skill-index row (line 88), §187 topic-table row ("Story Framework"), and the
  gotchas: `RegisterStories` → `AddSolTale`, `StoryManager` → `TaleManager`, `StoryJsonOptions` →
  `TaleJsonOptions`, `UseStoryRepository<T>` → `UseTaleRepository<T>`.
- `docs/theDesign.md` — EDIT — Story prose the original blast-radius missed: `SampleOrderWorkflowStory
  : StoryHandler<…>` (`:581`) → `SampleOrderWorkflowTale : TaleHandler<…>`, the `(Story orchestrator)`
  tree comment (`:179`) → "(Tale orchestrator)", and "Story" narrative mentions → "Tale". (The
  `CalculateBestPathTale` example here is already Tale.)
- `.github/skills/command-query-event-story/` → **`.github/skills/command-query-event-tale/`** —
  **RENAME (13b CONFIRMED, no longer optional).** Rename the folder; in `SKILL.md` update the
  frontmatter `name: command-query-event-story` → `command-query-event-tale` and the `# Command-Query-Event-Story`
  heading → `# Command-Query-Event-Tale`; sweep the Story-laden vocabulary (`SolTechnology.Core.Story`
  → `…Tale`, `StoryHandler` → `TaleHandler`, `RegisterStories` → `AddSolTale`, `StoryOptions` →
  `TaleOptions`, "a Story (chapters)"/"Stories" → "a Tale"/"Tales", the `Story.md` doc link → `Tale.md`,
  the DomainServices reference path `…/CityDomain/SaveCityStory/` → `…/CityDomain/SaveCityTale/` to
  match 05b); **fix pre-existing drift** `TellStory()` → the real `Tell()` (the `§4` doc-reference line).
  The cited `CalculateBestPathStory` drift has **zero** repo hits — the example is already
  `CalculateBestPathTale`; verify only, nothing to rename.
- **Every `command-query-event-story` cross-ref (verified repo-wide search — enumerate + repoint all):**
  `CLAUDE.md:88` (skill-index row, also names `.Story` package → `.Tale`), `docs/CQRS.md:162`,
  `docs/Story.md:545` + `:559` (move to `docs/Tale.md`), `docs/ClaudeCodingGuide.md:174` + `:227`. Also
  scan the skills/agents indexes (`.github/skills/README.md`, `.github/agents/README.md`) and any
  `routes to`/`see also` mentions in sibling skills for the old skill name.
- `docs/diagrams/README.md` + `docs/diagrams/story-framework-components.md` + `…/story-handle-sequence.md`
  — EDIT — `StoryHandler`/`StoryEngine` → `TaleHandler`/`TaleEngine`; consider renaming the two diagram
  files `story-*` → `tale-*` (documentation-cleanup call).
- Historical ADRs (`002-Story-Framework-Implementation.md`, `011-story-sqlite-extraction.md`) and their
  index titles — **leave as-is** (accepted records of past decisions; do not falsify history).
- `docs/Cron.md`, `docs/Guards.md`, `docs/Flow.md` — EDIT/DELETE — deprecated/ghost-topic disposition (see below).
- `docs/Clients.md` — VERIFY ONLY — step 09 owns its `HTTP` disposition; here we only confirm the links resolve.
- `.github/skills/package-management/references/canonical-versions.md` — EDIT — reconcile the phantom `SolTechnology.Core.Hangfire.Testing` row.

## Changes
- Fix `.slnx`: `docs\future-ideas.md` (no such file — a `docs/future-ideas/` **folder** exists) and
  `docs\production-harvest-second-app.md` (does not exist) — remove or repoint. Normalise the
  mixed `\` vs `/` separators in the Docs folder while there.
- **`Story → Tale` prose pass (decision 13 — owned here).** This step is the single coherent home for
  every Story→Tale **documentation** change, because the `command-query-event-story` skill cites
  `ClaudeCodingGuide.md` as authoritative ("the guide wins — fix it in the same PR", §19), so the
  skill and the guide must move **together** — and prose does not affect build-greenness, so it is
  safe to land after the step-05b code wave. Execute via `documentation-cleanup`:
  (1) move `docs/Story.md` → `docs/Tale.md` (rewrite all examples to `TaleHandler`/`AddSolTale`/`…Tale`),
  dispose of `Story.md`, and **flip** the `SolTechnology.Core.Tale.csproj` readme include to
  `docs\Tale.md` in the same change;
  (2) update `docs/ClaudeCodingGuide.md` §0/§3/§4 and `CLAUDE.md` (skill row, topic table, gotchas);
  (3) **rename** the skill `command-query-event-story → command-query-event-tale` (13b CONFIRMED —
  folder + `name:` + heading), sweep its Story-laden vocabulary (incl. the `…/SaveCityStory/` →
  `…/SaveCityTale/` reference path), repoint **every** cross-ref enumerated above
  (`CLAUDE.md:88`, `docs/CQRS.md:162`, `docs/Story.md:545`/`:559`→`Tale.md`, `docs/ClaudeCodingGuide.md:174`/`:227`),
  and fix the `TellStory()` → `Tell()` drift (`CalculateBestPathStory` has no repo hits — already
  `CalculateBestPathTale`, verify only);
  (4) update `docs/diagrams` (`StoryHandler`/`StoryEngine` → `TaleHandler`/`TaleEngine`; optional
  `story-*` → `tale-*` file renames);
  (5) leave historical ADRs (002, 011) + their index titles untouched.
- **Deprecated/ghost-topic docs disposition.** The maintainer pinned only `Clients.md` (owned by step
  09). For the rest, the recommended disposition (aligned with the "deprecate + stop packing" answers,
  to be confirmed during the `documentation-cleanup` run):
  - `docs/Cron.md` (Scheduler) → migration landing page pointing to `docs/Hangfire.md` /
    `AddSolRecurringJob<T>`.
  - `docs/Guards.md` → migration landing page pointing to FluentValidation `AbstractValidator<T>`.
  - `docs/Flow.md` (out-of-repo `SolTechnology.Core.Flow`) → delete, or a one-line "moved out of repo"
    landing page.
  - Either way: no orphan deprecated-topic doc may be silently live.
- Reconcile `canonical-versions.md`: it references `SolTechnology.Core.Hangfire.Testing` (using
  `Hangfire.InMemory 1.0.0`), but no such project exists in `src/` or `.slnx` — remove the row or
  flag it as planned. (Step 02 separately adds the "SourceLink is built-in" note here.)
- Run the `documentation-cleanup` skill over `docs/` + `README.md` + `dontreadme.md` for: module↔doc
  parity (note the `SolTechnology.Core.Api` folder vs `SolTechnology.Core.API.csproj`/namespace casing
  wart), link integrity, table alignment, Mermaid validity, ADR formatting.

## Acceptance criteria
- [ ] `SolTechnology.Core.slnx` has no `<File>` entry pointing to a non-existent path.
- [ ] `docs/Tale.md` exists with all examples on `TaleHandler`/`AddSolTale`/`…Tale`; `docs/Story.md` is
      deleted or a working redirect; `SolTechnology.Core.Tale.csproj` packs `docs\Tale.md` as its readme.
- [ ] No `StoryHandler` / `RegisterStories` / "Story Framework" / `IStoryRepository` / `SaveCityStory`
      remains in `docs/ClaudeCodingGuide.md`, `CLAUDE.md`, `docs/theDesign.md`, the renamed
      `command-query-event-tale` skill, or `docs/diagrams` (historical ADRs 002/011 excepted);
      `TellStory()`→`Tell()` drift fixed (`CalculateBestPathStory` confirmed absent — already Tale).
- [ ] **13b applied (CONFIRMED rename):** the skill folder + `name:` + heading are
      `command-query-event-tale`, its vocabulary is Tale, and **every** cross-ref (`CLAUDE.md:88`,
      `docs/CQRS.md:162`, `docs/Tale.md`, `docs/ClaudeCodingGuide.md:174`/`:227`, skills/agents indexes)
      resolves — no dangling `command-query-event-story` reference anywhere.
- [ ] Every `docs/*.md` either documents a shipped package or is an explicit, linked migration/landing
      page; no orphan deprecated-topic doc is silently live.
- [ ] `docs/Clients.md` (disposition set by step 09) resolves with no broken links.
- [ ] `canonical-versions.md` no longer lists a non-existent project as if it ships.
- [ ] `documentation-cleanup` skill reports zero broken intra-repo links and valid Mermaid.

## Open questions
- **13b — RESOLVED → rename the skill** `command-query-event-story → command-query-event-tale` (folder
  + heading + `name:` + every enumerated cross-ref). No longer a choice; this step executes the rename.
- Keep-as-migration-landing vs delete for `Cron.md` / `Guards.md` / `Flow.md` — not pinned by the
  maintainer at step 00; recommended dispositions above, final call made during the
  `documentation-cleanup` run. (`Clients.md` is resolved — owned by step 09.)










