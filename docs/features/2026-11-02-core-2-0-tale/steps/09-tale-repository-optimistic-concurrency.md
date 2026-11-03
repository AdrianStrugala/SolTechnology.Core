---
spec: 2026-11-02-core-2-0-tale
step: 09
status: done
---

# Step 09: Tale — optimistic concurrency on `ITaleRepository.Save`

## Summary
The repository has no optimistic concurrency, so `CancelStory` is overwritten by the in-flight
engine's next save, concurrent resumes both run, and the idempotency key is stamped only after
the whole tale finishes (`cqrs-tale#8`, `TaleManager.cs:50,84,152`). Per design A3(a), `Save`
(signature from step 08) becomes a conditional write on a new `TaleInstance.Version` and returns
`ConflictError` on mismatch. The DreamTravel SQLite schema and mapping change in the same PR
(P-04), which makes this a persisted-contract step.

Former core step 41; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Tale/Models/TaleInstance.cs` — EDIT — NEW `public long Version { get; set; }` (`C-05`)
- `src/SolTechnology.Core.Tale/Persistence/InMemoryTaleRepository.cs` — EDIT — compare-and-swap on `Version`
- `src/SolTechnology.Core.Tale/Persistence/ITaleRepository.cs` — EDIT — XML doc of `Save` states the conditional-write contract
- `src/SolTechnology.Core.Tale/Orchestration/TaleManager.cs` — EDIT — handle `ConflictError`; stamp the idempotency key on the first save
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — increment `Version` per save; stop on `ConflictError`
- `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite/SQLiteTaleRepository.cs` — EDIT — `Version INTEGER NOT NULL DEFAULT 0` column (`CREATE TABLE` at `:84`, plus an `ALTER TABLE` for existing files); `UPDATE … WHERE Id = @Id AND Version = @Expected`
- `tests/SolTechnology.Core.Tale.Tests/InMemoryRepositoryTests.cs` — EDIT
- `tests/SolTechnology.Core.Tale.Tests/AdvancedScenariosTests.cs` — EDIT — exact `ConcurrentResume_OneWins_OthersConflict` (replaces the tautological test deleted in step 04)

## Changes
- On version mismatch `Save` returns `Result.Fail(new ConflictError(...))` and never throws.
- Tests:
  - `Save_StaleVersion_ReturnsConflict`.
  - `CancelStory_DuringRun_IsNotOverwritten`.
  - `StartStory_SameIdempotencyKeyConcurrently_StartsOnce`.
  - `ConcurrentResume_OneWins_OthersConflict`.
- DreamTravel SQLite migration test: an existing database file without `Version` is migrated on start (component test or a SQLite unit test; record which).

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green.
- [x] The DreamTravel build and component tests are green; the migration test is green.
- [x] The core brief's P-04 row matches the delivered behaviour and names the custom-repository migration; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into `docs/migration-2.0.md`.
- [x] Premortem mitigations for the persisted contract are applied.

## Open questions
- none (`C-05` confirmed by the user on 2026-11-02)

## Deviations

### `Version` semantics, the claim save, and test doubles

**Original plan:** `Save` becomes a conditional write on `TaleInstance.Version`; the engine
increments `Version` per save and stops on `ConflictError`; `TaleManager` stamps the idempotency
key on the first save.

**Actual implementation:** `Version` is the stored version the instance was read at (`0` = never
stored). `Save` writes only when the stored version (a missing tale counts as `0`) equals it, then
stores `Version + 1` and updates the passed instance, so the engine just keeps the value the
repository returns. A new tale is stored once, with its idempotency key, before the first chapter
runs; a second tale that reuses the key gets `ConflictError` from the repository (in-memory check
under a lock; SQLite unique index) and `TaleManager` returns the first tale instead. After a
conflict the engine runs no further chapters and skips the terminal save. Resuming a `Running` tale
stays allowed (crash recovery); concurrent resumes are serialized by the first save after the
interactive chapter. The three concurrency tests use a new test double, `GatedTaleRepository`,
which holds the first N reads until all N callers arrived and can run a hook before each save; all
three failed on `master` before the change (`ca4cab1c`). The SQLite migration test is the component
test `SQLiteRepositoryTests.Constructor_DatabaseWithoutVersionColumn_MigratesAndSavesConditionally`
(a database file created with the 1.x schema). The core brief's P-04 row and its `tale 09`
release-notes input now spell out the contract. The persisted-contract premortem items for this
step are RM-5 (documented in steps 10 and 13); no other `RM-n` targets step 09.

### Every persisted run claims the tale first; a `Running` tale cannot be resumed (fixed during step 10)

**Original plan:** As above; resuming a `Running` tale stayed allowed and only a new tale was
claimed before its first chapter.

**Actual implementation:** A full-suite run during step 10 made `ConcurrentResume_OneWins_OthersConflict`
fail once with two successes. A resume that read the tale after the winner's first save saw
`Running`, skipped the completed chapter, ran the rest, and reported success although its terminal
save conflicted. Fix (separate commit before step 10): every persisted run, new or resumed, first
saves the tale as `Running` with the version it read, so a concurrent run conflicts before any
chapter; `TaleManager.ResumeStory` returns `ConflictError` ("Tale is already running") for a
`Running` tale; the handler computes the result after the terminal save, so a conflicting terminal
save is reported. A tale left `Running` by a crashed host can be cancelled and restarted, but no
longer resumed. `GatedTaleRepository` now reads first and then waits, so all gated callers see the
same version; the regression test `Resume_WhileAnotherResumeRuns_ReturnsConflict` failed before the
fix, and the four concurrency tests passed 300 repetitions each after it.
