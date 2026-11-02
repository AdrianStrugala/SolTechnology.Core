---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 41
status: to-do
---

# Step 41: Tale — optimistic concurrency on `ITaleRepository.Save`

## Summary
The repository has no optimistic concurrency, so `CancelStory` is overwritten by the in-flight
engine's next save, concurrent resumes both run, and the idempotency key is stamped only after
the whole tale finishes (`cqrs-tale#8`, `TaleManager.cs:50,84,152`). Per design A3(a), `Save`
(signature from step 40) becomes a conditional write on a new `TaleInstance.Version` and returns
`ConflictError` on mismatch. The DreamTravel SQLite schema and mapping change in the same PR
(P-04), which makes this a persisted-contract step.

## Affected components
- `src/SolTechnology.Core.Tale/Models/TaleInstance.cs` — EDIT — NEW `public long Version { get; set; }` (`C-05`)
- `src/SolTechnology.Core.Tale/Persistence/InMemoryTaleRepository.cs` — EDIT — compare-and-swap on `Version`
- `src/SolTechnology.Core.Tale/Persistence/ITaleRepository.cs` — EDIT — XML doc of `Save` states the conditional-write contract
- `src/SolTechnology.Core.Tale/Orchestration/TaleManager.cs` — EDIT — handle `ConflictError`; stamp the idempotency key on the first save
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — increment `Version` per save; stop on `ConflictError`
- `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite/SQLiteTaleRepository.cs` — EDIT — `Version INTEGER NOT NULL DEFAULT 0` column (`CREATE TABLE` at `:84`, plus an `ALTER TABLE` for existing files); `UPDATE … WHERE Id = @Id AND Version = @Expected`
- `tests/SolTechnology.Core.Tale.Tests/InMemoryRepositoryTests.cs` — EDIT
- `tests/SolTechnology.Core.Tale.Tests/AdvancedScenariosTests.cs` — EDIT — exact `ConcurrentResume_OneWins_OthersConflict` (replaces the tautological test deleted in step 25)

## Changes
- On version mismatch `Save` returns `Result.Fail(new ConflictError(...))` and never throws.
- Tests:
  - `Save_StaleVersion_ReturnsConflict`.
  - `CancelStory_DuringRun_IsNotOverwritten`.
  - `StartStory_SameIdempotencyKeyConcurrently_StartsOnce`.
  - `ConcurrentResume_OneWins_OthersConflict`.
- DreamTravel SQLite migration test: an existing database file without `Version` is migrated on start (component test or a SQLite unit test; record which).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green.
- [ ] The DreamTravel build and component tests are green; the migration test is green.
- [ ] The brief's P-04 row matches the delivered behaviour and names the custom-repository migration; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into `docs/migration-2.0.md`.
- [ ] Premortem mitigations for the persisted contract are applied.

## Open questions
- none (`C-05` confirmed by the user on 2026-11-02)

## Deviations
