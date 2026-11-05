---
spec: 2026-11-02-core-2-0-sql
step: 03
status: done
---

# Step 03: SQL — transient-only connection retry, safer error translation, options validation

## Summary
`SQLConnectionFactory` and its surroundings have three defects:

- The factory retries every `Exception` with blocking sleeps, so a wrong password stalls a thread
  for about 39 s. It leaks the `SqlConnection` when the last attempt throws, and it uses a
  shared, non-thread-safe `static Random` (`infra#11`,
  `Connections/SQLConnectionFactory.cs:11,15-25`, Guide §9.6).
- `SqlErrorTranslator` copies `ex.Message` (constraint and table names) into `Description`, and
  its XML doc is wrong (`infra#22`).
- `ValidateOnStart()` has nothing to validate, and null arguments throw `ArgumentException`
  instead of `ArgumentNullException` (`infra#21`).

Former core step 33; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.SQL/Connections/SQLConnectionFactory.cs` — EDIT — retry only transient `SqlException.Number` values (`-2, 53, 1205, 4060, 40197, 40501, 40613, 49918, 49919, 49920`); dispose in `catch`; `Random.Shared`
- `src/SolTechnology.Core.SQL/SqlErrorTranslator.cs` — EDIT — `:47-48` doc; `:56-75` generic `Description` per error class
- `src/SolTechnology.Core.SQL/ModuleInstaller.cs` — EDIT — `:26-37` `.Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString))`; `ArgumentNullException.ThrowIfNull`
- `tests/SolTechnology.Core.SQL.Tests/SQLConnectionFactoryTests.cs` — NEW — uses `SQLFixture` (the `SQL.Testing` reference is added in step 02)
- `tests/SolTechnology.Core.SQL.Tests/SqlErrorTranslatorTests.cs` — NEW

## Changes
- An async `CreateConnection(CancellationToken)` overload is additive. It is not added here (scope); recorded as a follow-up.
- Tests:
  - `CreateConnection_LoginFailed_FailsWithoutRetry` (`SQLFixture` with a wrong password, elapsed < 2 s).
  - `Translate_UniqueViolation_DescriptionHasNoTableName`.
  - `AddSolSQL_EmptyConnectionString_HostStartFails`.
- Premortem RM-13 (core step 00, 2026-11-02, scenario 13): append a `P-nn` row to the core brief. An empty SQL `ConnectionString` now fails at host start instead of at the first connection, and the connection retry covers only the listed transient error numbers, so a non-transient failure surfaces immediately.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.SQL.Tests` is green (Docker).
- [x] No public or protected signature changed.
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [x] RM-13: the core brief's P-table has the SQL validation and retry row.

## Open questions
- none

## Deviations

### Tests run against the shared SQL Server and pin more cases

**Original plan:** three tests: `CreateConnection_LoginFailed_FailsWithoutRetry` with `SQLFixture`,
`Translate_UniqueViolation_DescriptionHasNoTableName`, and `AddSolSQL_EmptyConnectionString_HostStartFails`.

**Actual implementation:** `SqlException` has no public constructor, so the translator test inserts a duplicate row
into the shared `SQLTestEnvironment` database (new `InsertItem` helper) to get a real unique-key violation. The
host-start test also covers a whitespace connection string, and `AddSolSQL_NullConfiguration_ThrowsArgumentNullException`
pins the `ArgumentNullException`. Before the fix all five failed: no validation error at start (both cases), an
`ArgumentException`, a login failure after 41 s instead of under 2 s, and a `Description` with the table and
constraint names.

### P-23 row covers the whole step

**Original plan:** append a `P-nn` row for the SQL validation and retry change (RM-13).

**Actual implementation:** the row is `P-23`, because the planned `2026-11-04-cache-redis-reconnect` feature already
reserves `P-21` and `P-22`. It also names the `ArgumentNullException` and the fixed `Error.Description` texts, since
callers that show `Description` see a different text. An unknown error number keeps its number in the description
("SQL Server returned error number N."), which carries no schema names.
