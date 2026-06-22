# ADR-011: Extract SQLite Story persistence out of `SolTechnology.Core.Story` into the DreamTravel sample

> **Status:** Proposed
> **Decision Date:** 2026-06-22
> **Decision Maker:** Repository maintainers
> **Stakeholders:** Story framework consumers, DreamTravel sample maintainers

## Context

`SolTechnology.Core.Story` ships a built-in SQLite persistence provider
(`SqliteStoryRepository` + `SqliteStoryRepositoryOptions` + two `UseSqliteStoryRepository(...)`
builder overloads). That provider drags two production `PackageReference`s into the library
(`Microsoft.Data.Sqlite.Core`, `SQLitePCLRaw.bundle_green`) plus a third, `Newtonsoft.Json`.

`SQLitePCLRaw.bundle_green` 2.1.11 pulls the native `SQLitePCLRaw.lib.e_sqlite3` 2.1.11, which
carries **CVE-2025-6965 (GHSA-2m69-gcr7-jv3q, HIGH)** — a SQLite < 3.50.2 memory-corruption flaw.
A CVE re-validation on 2026-06-22 confirms **no patched `SQLitePCLRaw` release exists yet**
(2.1.11 is still the latest), so the advisory cannot be cleared by a version bump.

The `src/` projects build with `TreatWarningsAsErrors=true` and only demote NU1900/NU1510
(`src/Directory.Build.props`), so NU1903 (the CVE audit error) would **fail the build**. Today it
is masked by a `<NuGetAuditSuppress>` in both `src/SolTechnology.Core.Story/SolTechnology.Core.Story.csproj`
and `tests/SolTechnology.Core.Story.Tests/SolTechnology.Core.Story.Tests.csproj`. Suppressing a HIGH
CVE in a published library is undesirable: every consumer of `SolTechnology.Core.Story` inherits the
vulnerable native dependency whether or not they ever touch SQLite. The in-memory repository is the
default; SQLite is opt-in, yet the cost is paid by all.

**Goal:** remove the SQLite dependency from the library entirely. The library keeps only the
persistence *contract* and the dependency-free default:

- `IStoryRepository` (interface)
- `InMemoryStoryRepository` (default)
- `UseInMemoryStoryRepository()`
- `UseStoryRepository<TRepository>(ServiceLifetime)` (the generic escape hatch)

SQLite becomes a **consumer concern**, demonstrated in the DreamTravel sample under a new DataLayer
project. This is pre-1.0 (`Story` is at `0.7.0`), so a breaking removal is acceptable.

### Affected modules

- `src/SolTechnology.Core.Story` — loses the SQLite repo, options, builder overloads, three packages,
  and the audit suppression.
- `tests/SolTechnology.Core.Story.Tests` — loses the SQLite test file, the two SQLite registration
  cases, and the audit suppression.

### Affected sample apps

- `sample-tale-code-apps/DreamTravel` — gains a new `DataLayer` project hosting the relocated SQLite
  provider, plus a new unit-test project. No running host changes behaviour (every host uses the
  in-memory default today — verified: the only `UseSqliteStoryRepository` mention in DreamTravel is a
  doc comment in `DreamTravel.Workflows/ModuleInstaller.cs`, not a call site).

### Planning findings (verified during this ADR)

1. **The public seam is already sufficient — no new public API is required.** A consumer-side
   `UseSqliteStoryRepository` can be re-implemented entirely against the *public* `IStoryBuilder`
   surface: register `SqliteStoryRepositoryOptions` as a singleton into `builder.Services`, then call
   the existing public `UseStoryRepository<SqliteStoryRepository>(ServiceLifetime.Singleton)`. The
   built-in DI container selects the `SqliteStoryRepository(SqliteStoryRepositoryOptions? options = null)`
   constructor and injects the registered options; `UseStoryRepository<T>()` already performs the
   `RemoveAll<IStoryRepository>()` + `EnsureStoryManager` wiring the original private helpers did.
2. **`Newtonsoft.Json` 13.0.4 in `Story.csproj` is unused.** There are zero `Newtonsoft`/`JsonConvert`
   references anywhere in `src/SolTechnology.Core.Story`. The SQLite repo and the engine both use
   `System.Text.Json` (`StoryJsonOptions`). It is dead weight — **remove it from the library, do not
   move it** to the new project (which also does not need it).
3. **Package version correction.** The live `Story.csproj` pins `Microsoft.Data.Sqlite.Core` **10.0.9**
   (not 9.0.5 as stated in the task) and `SQLitePCLRaw.bundle_green` **2.1.11**. The new project must
   copy the versions verbatim from `Story.csproj` at implementation time.
4. **Pre-existing doc drift.** `docs/Story.md` (l.45, l.58), `CLAUDE.md` (l.265) and
   `docs/ClaudeCodingGuide.md` (l.224) reference `StoryOptions.WithSqlitePersistence(...)`,
   `WithInMemoryPersistence()`, `WithoutPersistence()` — **methods that do not exist** in
   `StoryOptions.cs` (which only has `StoryIdPrefix`, `RestrictControllerToRegisteredHandlers`,
   `Default`). The real API is `RegisterStories().UseSqliteStoryRepository(...)`. These docs are
   corrected to the actual builder API as part of this work.

## Decision

Move the SQLite implementation out of the library and into a new DreamTravel DataLayer project,
re-wiring it through the **existing public builder seam** rather than adding new public API.

1. **New project** `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite` hosts:
   - the relocated `SQLiteStoryRepository` and `SQLiteStoryRepositoryOptions`
     (`System.Text.Json`-based, unchanged logic, renamed to ADR-001 acronym casing), and
   - a consumer-side `UseSQLiteStoryRepository(...)` extension built on the public
     `IStoryBuilder.Services` + `UseStoryRepository<SQLiteStoryRepository>()` seam.
   - It carries the SQLite `PackageReference`s and an **explicit, commented**
     `<NuGetAuditSuppress GHSA-2m69-gcr7-jv3q>` (DreamTravel inherits warning-level audit, so this is
     hygiene/output-cleanliness, not a build gate). `ProjectReference` to `SolTechnology.Core.Story`.
2. **Library shrinks** to `IStoryRepository` + `InMemoryStoryRepository` + `UseInMemoryStoryRepository()`
   + `UseStoryRepository<T>()`. The three SQLite-related packages, the audit suppression, and the
   unused `Newtonsoft.Json` are removed. The private `ReplaceRepository`/`EnsureStoryManager` helpers
   stay (still used by `UseInMemoryStoryRepository` / `UseStoryRepository<T>`).
3. **Tests relocate** to a new `DreamTravel.SQLite.UnitTests` project. `SQLiteRepositoryTests`
   moves (renamed from `SqliteRepositoryTests`; it only touches public `Story` types). The two
   `UseSqliteStoryRepository_*` cases in `RegistrationDefaultsTests` move and are adapted to assert the
   consumer extension's repository swap + DB-file creation + options registration (the full pause/resume
   cycle stays covered by the surviving in-memory case and the generic `UseStoryRepository<T>` case in
   Story.Tests).
4. **Docs repoint** SQLite references to the sample and correct the phantom `WithSqlitePersistence`
   API to the real builder API.
5. **No running DreamTravel host is flipped to SQLite** (see Consequences → rationale). The provider
   ships as an available, tested, documented option.

The work is sequenced so that **every project stays green at every step**: the new home is fully built
and tested *before* the library's copies are deleted, and the library's code removal (logic) is
separated from its package/suppress removal (plumbing).

## Alternatives Considered

1. **Keep SQLite in the library; keep suppressing the CVE (status quo).**
   *Pros:* zero work; SQLite stays one-line discoverable. *Cons:* every consumer inherits a HIGH CVE
   and a native dependency they may never use; suppressing a HIGH advisory in a published package is a
   poor security posture and an ongoing maintenance flag. Rejected — does not meet the goal.

2. **Extract SQLite into a new *core* package `SolTechnology.Core.Story.Sqlite`.**
   *Pros:* reusable across consumers, not just DreamTravel; clean opt-in package boundary; mirrors the
   `.Testing` companion pattern (ADR-008). *Cons:* a *new published library still carries the
   unpatched HIGH CVE* and would need its own suppression — it relocates the problem into the product
   surface instead of removing it; adds a package to publish/version/maintain; heavier than the task
   asks. Rejected for now — but recorded as the natural future move if a reusable SQLite provider is
   ever wanted *after* `SQLitePCLRaw` ships a patched bundle.

3. **Add a new public seam to `IStoryBuilder` (e.g. `ReplaceRepository`/`EnsureStoryManager` made
   public) for the consumer extension to call.**
   *Pros:* lets the consumer extension mirror the original internals exactly. *Cons:* unnecessary — the
   existing public `UseStoryRepository<T>()` already does precisely this wiring; widening the public
   surface adds a maintenance liability for no gain. Rejected (finding #1).

4. **Chosen: relocate to a DreamTravel sample DataLayer project, reusing the existing public seam.**
   *Pros:* removes the CVE from every published library; demonstrates the "bring-your-own-backend"
   story the framework already advertises; no new public API; no new published package. *Cons:* SQLite
   is no longer one-line-discoverable from the package itself — consumers must copy ~340 lines from the
   sample (mitigated by docs pointing at the sample). Breaking change (acceptable pre-1.0).

## Consequences

**Positive**

- `SolTechnology.Core.Story` and `SolTechnology.Core.Story.Tests` carry **zero** SQLite packages and
  **zero** audit suppressions; NU1903 disappears from `src/`.
- The library's dependency graph shrinks by three packages (incl. the unused `Newtonsoft.Json`).
- The framework's "any `IStoryRepository`, plug it via `UseStoryRepository<T>()`" promise gains a
  real, tested reference implementation in a consumer.
- No public-API growth; the existing `UseStoryRepository<T>()` seam is validated as the extension point.

**Negative**

- Breaking change: `SqliteStoryRepository`, `SqliteStoryRepositoryOptions`, and the two
  `UseSqliteStoryRepository(...)` overloads leave the public surface of `SolTechnology.Core.Story`.
  Any external consumer calling them must vendor the provider (copy from the sample). Acceptable
  pre-1.0; called out in the release notes.
- SQLite persistence is no longer discoverable from the package alone; discoverability moves to
  `docs/Story.md` + the sample.
- The DreamTravel sample (and its new test project) now carry the SQLite packages and the (warning-level)
  CVE; an explicit commented suppression keeps the sample build output clean and documents the known risk.

**Rationale — why no running host is flipped to SQLite:** wiring a live DreamTravel host (e.g. the
Worker) to `.UseSqliteStoryRepository(...)` would (a) introduce a file-DB runtime dependency into the
sample's run/CI path and (b) re-introduce the suppressed HIGH CVE into that host's audit surface. The
demonstration value is fully delivered by the new project + its extension + tests + docs. If a live
demo is later wanted, the Worker host is the natural place (durable persistence for background-processed
stories) and it is a single `.UseSqliteStoryRepository(...)` line in its composition root — deferred as
a follow-up the user can request.

**Semver impact:** **MINOR** for `SolTechnology.Core.Story` (`0.7.0` → `0.8.0`). Breaking public-API
removal, permitted under 0.x semantics; would be MAJOR post-1.0.

## Resolved Decisions

> Both open questions were answered by the user on 2026-06-22. Decisions are binding for implementation.

1. **Project name (NAMING) — RESOLVED.** Governed by [ADR-001](001-acronym-capitalization-refactoring.md)
   (acronyms are ALL CAPS). `SQL` is an acronym, so the name is **`DreamTravel.SQLite`** — `SQL` uppercase,
   `ite` lowercase (this also matches SQLite's own branding). *Not* `SQLLite` (double-L typo) and *not*
   `Sqlite` (lowercases the acronym, violating ADR-001). The sibling `DreamTravel.Sql` predates ADR-001 and
   is grandfathered; this **new** project follows the rule. ADR-001 applies to the relocated public symbols
   too: `SqliteStoryRepository` → **`SQLiteStoryRepository`**, `SqliteStoryRepositoryOptions` →
   **`SQLiteStoryRepositoryOptions`**, `UseSqliteStoryRepository` → **`UseSQLiteStoryRepository`**, and the
   test class → **`SQLiteRepositoryTests`**. External package IDs (`Microsoft.Data.Sqlite.Core`,
   `SQLitePCLRaw.bundle_green`) are unchanged. Folder, csproj, assembly, and root namespace are all
   `DreamTravel.SQLite`.
2. **Phantom persistence API (DOC) — RESOLVED.** Confirmed as a **documentation fix**. Correct the docs to
   the real `RegisterStories().UseSQLiteStoryRepository(...)` builder API. Do **not** implement the
   non-existent `StoryOptions.WithSqlitePersistence(...)` / `WithInMemoryPersistence()` /
   `WithoutPersistence()` factory methods — they stay out of scope.

## Related

- [ADR-002](002-Story-Framework-Implementation.md) — original Story framework design; documents the
  built-in SQLite provider this ADR relocates.
- [ADR-008](008-testing-framework-companions.md) — the `.Testing` companion-package pattern referenced
  in Alternative 2.
- [ADR-006](006-implementation-plan-workflow.md) — the plan workflow this document follows.
- `docs/Story.md` §Registration / §Persistence — consumer-facing persistence docs corrected here.
- CVE-2025-6965 / [GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q) — the
  driving advisory (HIGH, no patched release as of 2026-06-22).



