---
spec: 2026-11-02-core-2-0-tale
step: 08
status: to-do
---

# Step 08: Tale — `ITaleRepository` 2.0 signatures (cancellation and names)

## Summary
The repository contract has no `CancellationToken` (`cqrs-tale#9`), and it puts `Async` suffixes
on `ListAsync`/`SaveAsync`/`DeleteAsync` but not on `FindById` (`cqrs-tale#28`, Guide §10). This
step ships the final 2.0 signatures of `C-05` in one go, including `Task<Result> Save(...)`, so
custom repositories change their signatures once. `Save` always returns success here; step 09
adds `TaleInstance.Version` and the conditional write that can return `ConflictError` (split from
the former single repository step during plan review, RQ11).

Former core step 40; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Tale/Persistence/ITaleRepository.cs` — EDIT — `:12,19,25,33,36` `C-05` signatures
- `src/SolTechnology.Core.Tale/Persistence/InMemoryTaleRepository.cs` — EDIT — renames, tokens, `Task<Result>` from `Save`
- `src/SolTechnology.Core.Tale/Orchestration/TaleManager.cs` — EDIT — pass tokens; check the `Save` result
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — pass tokens; check the `Save` result
- `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite/SQLiteTaleRepository.cs` — EDIT — new signatures; tokens passed to `ExecuteNonQueryAsync`/`ExecuteReaderAsync`
- `tests/SolTechnology.Core.Tale.Tests/InMemoryRepositoryTests.cs` — EDIT — renamed calls
- `tests/SolTechnology.Core.Tale.Tests/**` — EDIT — any other test double of `ITaleRepository`

## Changes
- Signatures:
  - `Task<TaleInstance?> FindById(Auid taleId, CancellationToken cancellationToken)`.
  - `Task<TaleInstance?> FindByIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken)`.
  - `Task<IReadOnlyList<TaleInstance>> List(..., CancellationToken cancellationToken)`.
  - `Task<Result> Save(TaleInstance taleInstance, CancellationToken cancellationToken)`.
  - `Task Delete(Auid taleId, CancellationToken cancellationToken)`.
- Pure rename plus tokens; no `[Obsolete]` forwarders (`naming-and-public-api.md`).
- Test: `FindById_CancelledToken_Throws` (in-memory repository honours the token).

## Acceptance criteria
- [ ] `grep -rnE "\b(ListAsync|SaveAsync|DeleteAsync)\(" src/SolTechnology.Core.Tale sample-tale-code-apps/DreamTravel/src tests/SolTechnology.Core.Tale.Tests` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green.
- [ ] `dotnet build SolTechnology.Core.slnx` is green, and the DreamTravel component tests are green.

## Open questions
- none (`C-05` confirmed by the user on 2026-11-02)

## Deviations
