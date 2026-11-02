---
spec: 2026-11-02-logging-chronicle
step: 07
status: blocked
---

# Step 07: Chronicle SQL steps — SqlClient `DiagnosticListener` observer

## Summary
SQL queries become steps without changing `SolTechnology.Core.SQL` (design B3). A Logging-owned
hosted service subscribes to `DiagnosticListener.AllListeners`. It attaches to the listener named
`SqlClientDiagnosticListener` and maps `Microsoft.Data.SqlClient.WriteCommandBefore`, `…After`,
and `…Error` events to leaf steps under the ambient step. Because it observes SqlClient itself,
it captures Dapper over `ISQLConnectionFactory`, EF Core, and raw `SqlConnection` alike. Command
text is not recorded by default (PII). A per-statement hash serves as the group key, so N+1
aggregation counts only identical statements. The `IsEnabled` predicate returns `false` outside
roots, so SqlClient creates no payloads there.

## Affected components
- `src/SolTechnology.Core.Logging/Chronicles/SQL/ChronicleSQLClientObserver.cs` — NEW — `internal sealed`, `IObserver<DiagnosticListener>` + `IObserver<KeyValuePair<string, object?>>`
- `src/SolTechnology.Core.Logging/Chronicles/SQL/SQLClientPayloadReader.cs` — NEW — internal; cached `PropertyInfo` per payload type for `Command`, `Exception`; `CommandText`, `CommandType` on the command object; no `Microsoft.Data.SqlClient` reference
- `src/SolTechnology.Core.Logging/Chronicles/SQL/ChronicleSQLClientSubscription.cs` — NEW — `internal sealed class … : IHostedService` (subscribe on start, dispose on stop)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRecorder.cs` — EDIT — internal `StartDetachedLeaf(string name, string groupKey)` (appends a child of the ambient node without changing the ambient node) and `Complete(leaf, status, error)`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptions.cs` — EDIT — `CaptureSQLCommands` (`true`), `CaptureSQLCommandText` (`false`) (CH-04)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleServiceRegistrations.cs` — EDIT — `TryAddEnumerable` hosted service
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleSQLClientObserverTests.cs` — NEW — synthetic `new DiagnosticListener("SqlClientDiagnosticListener")` writing anonymous payloads with `Command` objects that expose `CommandText`/`CommandType`
- `tests/SolTechnology.Core.SQL.Tests/SolTechnology.Core.SQL.Tests.csproj` — EDIT — `ProjectReference ..\..\src\SolTechnology.Core.Logging\SolTechnology.Core.Logging.csproj` (test-only); `PackageReference Microsoft.Extensions.Diagnostics.Testing` (same version as step 04)
- `tests/SolTechnology.Core.SQL.Tests/ChronicleSQLIntegrationTests.cs` — NEW — `SQLFixture` with the MSSQL engine (the `SQL.Testing` reference is added by `2026-11-02-core-2-0-sql` step "SQL — `Begin` on `IUnitOfWork`")

## Changes
- Pairing: a `ConditionalWeakTable<object, PendingSQLStep>` keyed by the command object from the
  payload, so there is no leak when `After` never arrives. `Before` captures the parent (the
  ambient node at `Before`, raised synchronously in the caller's flow); `After`/`Error`
  completes the step.
- Step name:
  - `CommandType.StoredProcedure` → `SQL {procedure name}`.
  - Otherwise → `SQL {first keyword in upper case}`, for example `SQL SELECT` or `SQL INSERT`.
- Group key: `name + "#" + CommandText.GetHashCode()`. This compares statements within one report
  only, so a randomized hash is acceptable. The statement text is never rendered unless
  `CaptureSQLCommandText`.
- With `CaptureSQLCommandText`, the attribute `Statement` holds the text cut to
  `MaxAttributeValueLength`. `Error` → `Fail(exceptionType, message)`.
- Logging still has no `ProjectReference` (cycle guard).
- Tests:
  - `Before_After_InsideRoot_RecordsLeafStep`.
  - `Error_RecordsFailedStep`.
  - `OutsideRoot_IsEnabled_ReturnsFalse`.
  - `CommandText_NotRecorded_ByDefault`.
  - `CaptureCommandText_RecordsTruncatedStatement`.
  - `FortyTwoIdenticalStatements_AggregatedAsNPlusOne`.
  - `DifferentStatements_NotAggregatedTogether`.
  - `CaptureDisabled_NoSubscription`.
  - `ChronicleSQLIntegrationTests.Query_InsideRoot_AppearsAsSQLStep` (real SqlClient 7.0.1 against MSSQL; settles crux B3-1).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [ ] `dotnet test tests/SolTechnology.Core.SQL.Tests --filter "FullyQualifiedName~ChronicleSQL"` is green with Docker available.
- [ ] `grep -n "ProjectReference" src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj` prints nothing.
- [ ] `git diff --stat -- src/SolTechnology.Core.SQL` for this step prints nothing.
- [ ] If the integration test shows that SqlClient 7.0.1 raises no events, this step is set `blocked` and alternative B3 (b) goes back to the user. This is a pass/fail gate, not a silent fallback.

## Open questions
- OQ-1 (brief): `CLAUDE.md §2` confirmation of `CH-04` (`CaptureSQLCommands`, `CaptureSQLCommandText`).

## Deviations
