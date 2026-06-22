# ADR-011: Extract SQLite Story persistence into the DreamTravel sample — Implementation Summary

Tracking the implementation steps for [ADR-011](../011-story-sqlite-extraction.md).

**Goal:** remove the unpatched HIGH CVE-2025-6965 (`SQLitePCLRaw.lib.e_sqlite3` 2.1.11) from the
published `SolTechnology.Core.Story` library by moving its SQLite persistence provider into a new
DreamTravel DataLayer project, leaving the library with `IStoryRepository` + `InMemoryStoryRepository`
+ `UseInMemoryStoryRepository()` + `UseStoryRepository<T>()` only.

**Sequencing invariant:** every project stays green at every step. The new home (Steps 01–04) is fully
built and tested *before* the library's SQLite code (Step 05) and packages/suppression (Step 06) are
removed. Code removal (logic) and package/suppress removal (plumbing) are separate PRs.

> ✅ **Open questions resolved (2026-06-22).** **OQ1 (naming):** per [ADR-001](../001-acronym-capitalization-refactoring.md)
> the project is **`DreamTravel.SQLite`** (`SQL` acronym uppercase); the relocated symbols follow suit —
> `SQLiteStoryRepository`, `SQLiteStoryRepositoryOptions`, `UseSQLiteStoryRepository`, `SQLiteRepositoryTests`.
> **OQ2 (docs):** confirmed a documentation fix — repoint to the real `RegisterStories().UseSQLiteStoryRepository(...)`
> builder API; do **not** implement the phantom `StoryOptions.WithSqlitePersistence(...)` methods.

> 🔍 **Reviewed (2026-06-22).** All nine steps reviewed against the codebase and moved to `reviewed/`.
> **Coverage decision = (c):** the engine↔SQLite pause/resume path is exercised by driving the real
> `SampleOrderWorkflowStory` through `StoryManager` on a real SQLite file DB (Step 04), with cases added
> for the happy full cycle + cross-instance durability, invalid customer input, and backend-chapter
> failure. The earlier plan to *drop* the full-cycle assertion is superseded. The new test project
> references both `DreamTravel.SQLite` and `DreamTravel.Workflows`. The existing in-memory, API-level
> `tests/Component/SampleOrderWorkflow/SampleOrderWorkflowTests.cs` stays unchanged (additive coverage).
> Premortem (Step 09) remains the gate before any code is written.

> ✅ **Premortem run — verdict: Go with mitigations (2026-06-22).** 10 scenarios scored; no H-severity
> risk without a plausible mitigation. The one unfilled gap — **bump `Story` `<Version>` 0.7.0 → 0.8.0** —
> is now folded into Step 06. Other required mitigations already live in Steps 04 (verify the actual
> post-invalid-input `StoryStatus`, don't assume), 07 (migration breadcrumb "copy `DreamTravel.SQLite`"),
> and 08 (`--force-evaluate` audit gate). Implementation may begin at Step 01.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | Scaffold the `DreamTravel.SQLite` project (csproj + packages + suppress + slnx) | [`done/01-scaffold-dreamtravel-sqlite-project.md`](done/01-scaffold-dreamtravel-sqlite-project.md) | ✅ done |
| 02 | Relocate + rename `SQLiteStoryRepository` + `SQLiteStoryRepositoryOptions` | [`done/02-relocate-sqlite-repository-and-options.md`](done/02-relocate-sqlite-repository-and-options.md) | ✅ done |
| 03 | Consumer-side `UseSQLiteStoryRepository` builder extension | [`reviewed/03-sqlite-story-builder-extension.md`](reviewed/03-sqlite-story-builder-extension.md) | 🔍 reviewed |
| 04 | New test project: relocate SQLite tests + SampleOrderWorkflowStory SQLite end-to-end coverage | [`reviewed/04-relocate-sqlite-tests.md`](reviewed/04-relocate-sqlite-tests.md) | 🔍 reviewed |
| 05 | Remove SQLite *code* from library + Story.Tests | [`reviewed/05-remove-sqlite-code-from-library.md`](reviewed/05-remove-sqlite-code-from-library.md) | 🔍 reviewed |
| 06 | Remove SQLite *packages* + suppression from `src/` | [`reviewed/06-remove-sqlite-packages-and-suppress.md`](reviewed/06-remove-sqlite-packages-and-suppress.md) | 🔍 reviewed |
| 07 | Repoint docs + fix phantom persistence API | [`reviewed/07-update-documentation.md`](reviewed/07-update-documentation.md) | 🔍 reviewed |
| 08 | Final verification (clean audit + green solution) | [`reviewed/08-final-verification.md`](reviewed/08-final-verification.md) | 🔍 reviewed |
| 09 | Run premortem (mandatory gate) | [`reviewed/09-run-premortem.md`](reviewed/09-run-premortem.md) | ✅ done — Go with mitigations |

Status values: `⬜ to-do` / `🔍 reviewed` / `✅ done`. Link in each row points to the step's current
location (`to-do/` / `reviewed/` / `done/`).

## Key planning findings (verified, not assumed)

- **No new public API needed.** The consumer extension rebuilds on the existing public
  `IStoryBuilder.Services` + `UseStoryRepository<SQLiteStoryRepository>()` seam; the container injects a
  DI-registered `SQLiteStoryRepositoryOptions` into the repo's optional-parameter constructor.
- **`Newtonsoft.Json` is dead weight in `Story.csproj`** (zero usages; the repo + engine use
  `System.Text.Json`) — removed, not moved.
- **Version correction:** `Microsoft.Data.Sqlite.Core` is **10.0.9** in the live csproj (task said 9.0.5);
  `SQLitePCLRaw.bundle_green` is **2.1.11**. Copy versions verbatim at implementation time.
- **CVE re-validated 2026-06-22:** GHSA-2m69-gcr7-jv3q (HIGH) still has **no patched** `SQLitePCLRaw`
  release — removal (not upgrade) is the only fix.
- **Non-breaking for the running sample:** every DreamTravel host uses the in-memory default today
  (the sole `UseSqliteStoryRepository` mention is a doc comment, not a call site).

