---
spec: 2026-11-02-core-2-0-sql
step: 02
status: to-do
---

# Step 02: SQL — `Begin` on `IUnitOfWork`

## Summary
DI registers `IUnitOfWork`, but `Begin()` exists only on the concrete `UnitOfWork`. Through the
interface, `Complete()` always throws and `Rollback()` throws `NullReferenceException` because
`_scope` is null (`B3`, `src/SolTechnology.Core.SQL/Transactions/IUnitOfWork.cs:3-7`,
`UnitOfWork.cs:9,29-31`). Calling `Begin()` twice also leaks the first `TransactionScope`. Per
design A4, `Begin` moves onto the interface (`C-15`).

Former core step 15; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.SQL/Transactions/IUnitOfWork.cs` — EDIT — add `Begin`, extend `IDisposable`
- `src/SolTechnology.Core.SQL/Transactions/UnitOfWork.cs` — EDIT — return type, guards
- `tests/SolTechnology.Core.SQL.Tests/UnitOfWorkTests.cs` — NEW
- `tests/SolTechnology.Core.SQL.Tests/SolTechnology.Core.SQL.Tests.csproj` — EDIT — `ProjectReference` to `src/SolTechnology.Core.SQL.Testing` (first use of `SQLFixture` under `tests/`; steps 03 and 04 reuse it)
- `sample-tale-code-apps/DreamTravel/**` — EDIT — only callers of `UnitOfWork.Begin` if any (`grep -rn "\.Begin(" sample-tale-code-apps`)

## Changes
- `public interface IUnitOfWork : IDisposable { IUnitOfWork Begin(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted); void Complete(); void Rollback(); }`
- `UnitOfWork`:
  - `Begin` throws `InvalidOperationException` when a scope is already open.
  - `Rollback` without `Begin` is a no-op.
  - `_scope` becomes `TransactionScope?`.
- Tests:
  - `Complete_ThroughInterface_AfterBegin_Commits` (uses `SQLFixture`).
  - `Rollback_WithoutBegin_DoesNotThrow`.
  - `Begin_Twice_Throws`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.SQL.Tests` is green (Docker for `SQLFixture`).
- [ ] The DreamTravel build is green.
- [x] The user confirmed `C-15` (2026-11-02).

## Open questions
- none (`C-15` confirmed by the user on 2026-11-02)

## Deviations
