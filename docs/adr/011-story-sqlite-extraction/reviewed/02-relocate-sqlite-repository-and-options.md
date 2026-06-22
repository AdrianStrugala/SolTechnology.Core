---
adr: 011-story-sqlite-extraction
step: 02 of 09
status: reviewed
---

# Step 02: Relocate + rename `SQLiteStoryRepository` + `SQLiteStoryRepositoryOptions` into the new project

## Summary
Move the SQLite repository implementation **and its options class together** (options ship with their
consumer) from the library into the project scaffolded in Step 01. The logic is copied unchanged
(`System.Text.Json`-based, WAL + retry behaviour preserved); the namespace changes to `DreamTravel.SQLite`
and the type identifiers are renamed to ADR-001 acronym casing (`Sqlite*` → `SQLite*`). This step
adds the **domain/logic** half of the provider; the DI extension (plumbing) follows in Step 03.

## Affected components
- `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite/SQLiteStoryRepository.cs` — new (from `src/SolTechnology.Core.Story/Persistence/SqliteStoryRepository.cs`, ~292 lines; class renamed `SqliteStoryRepository` → `SQLiteStoryRepository`)
- `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite/SQLiteStoryRepositoryOptions.cs` — new (from `src/SolTechnology.Core.Story/Persistence/SqliteStoryRepositoryOptions.cs`, ~44 lines; class renamed `SqliteStoryRepositoryOptions` → `SQLiteStoryRepositoryOptions`)

> The originals stay in the library for now — they are deleted in Step 05, after the new home and its
> tests are green. This guarantees no window where the code is gone but not yet relocated.

## Details
- Copy both files, then change the namespace from `SolTechnology.Core.Story.Persistence` to
  `DreamTravel.SQLite`, and rename the two public classes to `SQLiteStoryRepository` /
  `SQLiteStoryRepositoryOptions` (ADR-001: `SQL` is an acronym → ALL CAPS). Update all internal
  self-references (constructor names, the options field type, the static init `HashSet`).
- Add the necessary `using`s for the cross-assembly types now that they are no longer in the same
  namespace: `SolTechnology.Core.Story.Persistence` (for `IStoryRepository`) and
  `SolTechnology.Core.Story.Models` (for `StoryInstance`, `ChapterInfo`, `StoryStatus`).
  **`Auid` needs no `using` and no explicit AUID reference** — verified: `src/SolTechnology.Core.AUID/Auid.cs`
  declares `Auid` in the **global namespace**, and the AUID assembly flows transitively through Story's
  plain `ProjectReference`. Do not add an explicit `SolTechnology.Core.AUID` reference to the Step 01 csproj.
- Do **not** introduce `Newtonsoft.Json` — the repo serialises `History`/`CurrentChapter` with
  `System.Text.Json.JsonSerializer` (verified). No behavioural change.
- Keep both public constructors (`(SQLiteStoryRepositoryOptions? options = null)` and
  `(string connectionString)`); the consumer extension in Step 03 and the relocated tests in Step 04
  both depend on them.
- `SQLiteStoryRepositoryOptions` stays `public sealed` with its defaults intact (LocalApplicationData
  default connection string, WAL, retries) so the configure-callback overload works in Step 03.

## Acceptance criteria
- The new project builds with both files; `SQLiteStoryRepository : IStoryRepository` resolves against
  the `SolTechnology.Core.Story` `ProjectReference`.
- No `Newtonsoft` reference is introduced.
- Repository behaviour is identical to the original — the diff shows **only** namespace, identifier
  rename (`Sqlite*` → `SQLite*`), and `using` changes; no logic edits.

## Open questions
- None. OQ1 resolved: namespace `DreamTravel.SQLite`, types renamed to `SQLite*` per ADR-001.


