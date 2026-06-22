---
adr: 011-story-sqlite-extraction
step: 07 of 09
status: reviewed
---

# Step 07: Repoint documentation and fix the phantom persistence API

## Summary
Update the docs so SQLite is presented as a **consumer-provided** option living in the DreamTravel
sample, and correct the pre-existing references to the non-existent `StoryOptions.WithSqlitePersistence(...)`
/ `WithInMemoryPersistence()` / `WithoutPersistence()` API to the real
`RegisterStories().UseSQLiteStoryRepository(...)` builder API. Docs-only.

> ✅ **OQ2 resolved (2026-06-22).** Confirmed: this is a **documentation fix** to the real builder API.
> The phantom `StoryOptions.WithSqlitePersistence(...)` / `WithInMemoryPersistence()` /
> `WithoutPersistence()` methods are **not** implemented — they stay out of scope.

## Affected components
- `docs/Story.md` — **§Overview blurbs (l.~5, ~21)** that say "SQLite for production" with **no** method
  name (the grep-based acceptance below would miss these — reframe as "SQLite via the DreamTravel sample");
  §Registration (l.~42–69) phantom API; §Persistence (l.~421, ~424) examples; reference-impl note (l.~438);
  security note (l.~524)
- `CLAUDE.md` — troubleshooting row (l.~265) phantom API
- `docs/ClaudeCodingGuide.md` — `Workflows/` section (l.~224) phantom API
- `docs/adr/002-Story-Framework-Implementation.md` — light forward-pointer note where SQLite is mentioned (l.~90, ~109, ~132, ~140, ~152, ~155)
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Workflows/ModuleInstaller.cs` — doc comment (l.~12)
- `docs/adr/011-story-sqlite-extraction.md` + `docs/adr/README.md` — flip Status/Implementation when work completes (final bookkeeping, may also be done by `implement-plan`)

## Details
- **`docs/Story.md` §Overview (l.~5, ~21):** these blurbs frame SQLite as a built-in production option but
  contain no phantom method name — reframe them to "durable SQLite persistence is provided by the
  DreamTravel sample (`DreamTravel.SQLite`)". They escape the phantom-API grep, so call them out explicitly.
- **`docs/Story.md` §Registration:** replace the `StoryOptions.WithSqlitePersistence("stories.db")` /
  `WithoutPersistence()` / `WithInMemoryPersistence()` snippets with the real API:
  `services.RegisterStories();` (in-memory default), and a pointer that durable SQLite is provided by
  the sample `DreamTravel.SQLite` via `RegisterStories().UseSQLiteStoryRepository(...)`
  *once the sample's extension `using` is in scope*. The real signature is
  `RegisterStories(Action<StoryOptions>? configure = null, params Assembly[] assemblies)` — the phantom
  snippets pass a `StoryOptions` **instance** as the first arg, which is wrong twice over (the first arg
  is an `Action<StoryOptions>`, and `StoryOptions` has no such factory methods). Reconcile the "when
  persistence is enabled" wording with the real model: a repository is **always** present (in-memory is
  the default); there is no `WithoutPersistence` opt-out.
- **`docs/Story.md` §Persistence (l.~421/424):** keep the `UseSQLiteStoryRepository(...)` examples but
  frame them as "provided by the DreamTravel sample `DreamTravel.SQLite` — copy it for a
  production-grade persistent backend," and keep `UseStoryRepository<T>()` as the canonical, in-box
  extension mechanism.
- **`docs/Story.md` l.~438:** change "See `InMemoryStoryRepository` and `SqliteStoryRepository` for
  reference implementations" → `InMemoryStoryRepository` (in-box) and the sample's `SQLiteStoryRepository`.
- **`docs/Story.md` l.~524:** repoint the `SqliteStoryRepository` security note to the sample's
  `SQLiteStoryRepository` provider.
- **`CLAUDE.md` l.~265:** rewrite the troubleshooting row to the real model — a repository is always
  present (in-memory default); interactive stories need `RegisterStories()` (no opt-out method); durable
  SQLite is the sample's `UseSQLiteStoryRepository(...)`. Remove the phantom method names.
- **`docs/ClaudeCodingGuide.md` l.~224:** replace `RegisterStories(StoryOptions.WithSqlitePersistence(...))`
  with "require a persisted `IStoryRepository` — e.g. the DreamTravel sample's
  `RegisterStories().UseSQLiteStoryRepository(...)`, or any `UseStoryRepository<T>()` backend."
- **`docs/adr/002-...md`:** do **not** rewrite history. Add a brief note (e.g. a `> Note (ADR-011):`
  line) at the SQLite mentions stating the reference SQLite provider now lives in the DreamTravel sample.
- **`DreamTravel.Workflows/ModuleInstaller.cs` l.~12:** update the XML doc comment example to reference
  the sample's `UseSQLiteStoryRepository(...)` (now provided by `DreamTravel.SQLite`), or the generic
  `UseStoryRepository<T>()`. Comment-only — no logic change.

## Acceptance criteria
- `grep -rn "WithSqlitePersistence\|WithInMemoryPersistence\|WithoutPersistence" docs CLAUDE.md` returns nothing.
- Every remaining `UseSQLiteStoryRepository` mention in `docs/` frames it as a sample/consumer concern.
- `docs/Story.md` describes the real builder API only; no phantom `StoryOptions` factory methods remain.
- Run the `documentation-cleanup` skill: links, tables, and module/doc parity still pass.

## Open questions
- OQ2 (phantom API) — gates whether this is a doc fix (assumed) or a code change (out of scope).

