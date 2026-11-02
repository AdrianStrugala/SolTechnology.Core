---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 33
status: to-do
---

# Step 33: SQL — transient-only connection retry, safer error translation, options validation

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

## Affected components
- `src/SolTechnology.Core.SQL/Connections/SQLConnectionFactory.cs` — EDIT — retry only transient `SqlException.Number` values (`-2, 53, 1205, 4060, 40197, 40501, 40613, 49918, 49919, 49920`); dispose in `catch`; `Random.Shared`
- `src/SolTechnology.Core.SQL/SqlErrorTranslator.cs` — EDIT — `:47-48` doc; `:56-75` generic `Description` per error class
- `src/SolTechnology.Core.SQL/ModuleInstaller.cs` — EDIT — `:26-37` `.Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString))`; `ArgumentNullException.ThrowIfNull`
- `tests/SolTechnology.Core.SQL.Tests/SQLConnectionFactoryTests.cs` — NEW — uses `SQLFixture` (the `SQL.Testing` reference is added in step 15)
- `tests/SolTechnology.Core.SQL.Tests/SqlErrorTranslatorTests.cs` — NEW

## Changes
- An async `CreateConnection(CancellationToken)` overload is additive. It is not added here (scope); recorded as a follow-up.
- Tests:
  - `CreateConnection_LoginFailed_FailsWithoutRetry` (`SQLFixture` with a wrong password, elapsed < 2 s).
  - `Translate_UniqueViolation_DescriptionHasNoTableName`.
  - `AddSolSQL_EmptyConnectionString_HostStartFails`.
- Premortem RM-13 (step 00, 2026-11-02, scenario 13): append a `P-nn` row to the brief. An empty SQL `ConnectionString` now fails at host start instead of at the first connection, and the connection retry covers only the listed transient error numbers, so a non-transient failure surfaces immediately.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.SQL.Tests` is green (Docker).
- [ ] No public or protected signature changed.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [ ] RM-13: the brief's P-table has the SQL validation and retry row.

## Open questions
- none

## Deviations
