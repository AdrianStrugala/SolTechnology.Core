---
spec: 2026-11-02-logging-chronicle
step: 07
status: in-progress
---

# Step 07: Chronicle SQL steps — SqlClient `DiagnosticListener` observer

## Summary
SQL queries become steps without changing `SolTechnology.Core.SQL` (design B3). A Logging-owned
hosted service subscribes to `DiagnosticListener.AllListeners`. It attaches to the listener named
`SqlClientDiagnosticListener` and maps `Microsoft.Data.SqlClient.WriteCommandBefore`, `…After`,
and `…Error` events to leaf steps under the ambient step. Because it observes SqlClient itself,
it captures Dapper over `ISQLConnectionFactory`, EF Core, and raw `SqlConnection` alike. Command
text is not recorded by default (PII). A per-statement hash serves as the group key, so N+1
aggregation counts only identical statements. The `IsEnabled` predicate returns `false` for
`WriteCommandBefore` outside roots, so SqlClient creates no payloads there. Event names, the
listener name, and the payload shape were verified against the SqlClient `v7.0.1` source by the
plan review (brief, Current state).

## Affected components
- `src/SolTechnology.Core.Logging/Chronicles/SQL/ChronicleSQLClientObserver.cs` — NEW — `internal sealed`, `IObserver<DiagnosticListener>` + `IObserver<KeyValuePair<string, object?>>`
- `src/SolTechnology.Core.Logging/Chronicles/SQL/SQLClientPayloadReader.cs` — NEW — internal; reads `OperationId` (`Guid`), `Command` (as `System.Data.IDbCommand`: `CommandText`, `CommandType`), and `Exception` by enumerating the payload as `IReadOnlyList<KeyValuePair<string, object>>`; no reflection and no `Microsoft.Data.SqlClient` reference
- `src/SolTechnology.Core.Logging/Chronicles/SQL/ChronicleSQLClientSubscription.cs` — NEW — `internal sealed class … : IHostedService` (subscribe on start, dispose on stop)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRecorder.cs` — EDIT — internal `StartDetachedLeaf(string name, string groupKey)` (appends a child of the ambient node without changing the ambient node) and `Complete(leaf, status, error)`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRoot.cs` — EDIT — internal `Owner` (the recorder that started the root; CRM-1)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptions.cs` — EDIT — `CaptureSQLCommands` (`true`), `CaptureSQLCommandText` (`false`) (CH-04)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleServiceRegistrations.cs` — EDIT — `TryAddEnumerable` hosted service
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleSQLClientObserverTests.cs` — NEW — synthetic `new DiagnosticListener("SqlClientDiagnosticListener")` writing `KeyValuePair<string, object>` lists shaped like the 7.x payloads (`OperationId`, `Command` = NSubstitute `IDbCommand`, `Exception`); each test disposes its subscription in `TearDown` (the `AllListeners` subscription is process-wide)
- `tests/SolTechnology.Core.SQL.Tests/SolTechnology.Core.SQL.Tests.csproj` — EDIT — `ProjectReference ..\..\src\SolTechnology.Core.Logging\SolTechnology.Core.Logging.csproj` (test-only); `PackageReference Microsoft.Extensions.Diagnostics.Testing` (version from `Directory.Packages.props`, added by step 04)
- `tests/SolTechnology.Core.SQL.Tests/ChronicleSQLIntegrationTests.cs` — NEW — uses the shared SQL Server container of `SQLTestEnvironment.Database()` (`SQLFixture`, default `SQLProvider.SQLServer`; `SQL.Tests` already references `SQL.Testing`); builds a `ServiceProvider` like `SQLTestEnvironment.BuildProvider()` plus `AddSolChronicle()` and calls `StartAsync` on the registered `IHostedService`s, because the subscription lives in a hosted service

## Changes
- Pairing: a `ConditionalWeakTable<object, PendingSQLStep>` keyed by the command object
  (`AddOrUpdate`, so a re-executed command replaces its stale entry), and the entry must match
  the payload's `OperationId`. There is no leak when `After` never arrives. `Before` captures the
  parent (the ambient node at `Before`, raised synchronously in the caller's flow); `After`/`Error`
  completes the step. A payload with `OperationId == Guid.Empty` (SqlClient writes it when
  `Before` was disabled) is ignored.
- `IsEnabled` predicate (SqlClient calls the single-argument `IsEnabled(eventName)` before every
  write):
  - `Microsoft.Data.SqlClient.WriteCommandBefore` → `true` only inside an open root.
  - `…WriteCommandAfter` / `…WriteCommandError` → `true` while any pending SQL step exists
    (`Interlocked` counter), never gated on the ambient root, because the async completion may run
    after the root closed or outside its flow. A root-gated `After` would leave the step
    `(unfinished)`.
  - Every other SqlClient event (connection open, transaction) → `false`.
- Step name:
  - `CommandType.StoredProcedure` → `SQL {procedure name}`.
  - Otherwise → `SQL {first keyword in upper case}`, for example `SQL SELECT` or `SQL INSERT`.
- Group key: `name + "#" + CommandText.GetHashCode()`. This compares statements within one report
  only, so a randomized hash is acceptable. The statement text is never rendered unless
  `CaptureSQLCommandText`.
- With `CaptureSQLCommandText`, the attribute `Statement` holds the text cut to
  `MaxAttributeValueLength`. `Error` → `Fail(exceptionType, message)`.
- Logging still has no `ProjectReference` (cycle guard).
- Connection retry (2.0 SQL, `docs/architecture/infrastructure-modules.md` "Connections and
  retry"): `ISQLConnectionFactory.CreateConnection()` opens synchronously and retries a transient
  `SqlException` up to three times with about 40 seconds of delays. Opens are not command events,
  so that time shows as a gap inside the parent step, not as a `SQL` step. Step 15 documents it.
- `Microsoft.Identity.Client` (pinned for DacFx) and the absent MSAL in SqlClient 7 do not affect
  the command events; the integration test stays the binding evidence for `Microsoft.Data.SqlClient`
  7.0.1 (`Directory.Packages.props`).
- Tests:
  - `Before_After_InsideRoot_RecordsLeafStep`.
  - `Error_RecordsFailedStep`.
  - `OutsideRoot_IsEnabled_ReturnsFalse`.
  - `CommandText_NotRecorded_ByDefault`.
  - `CaptureCommandText_RecordsTruncatedStatement`.
  - `FortyTwoIdenticalStatements_AggregatedAsNPlusOne`.
  - `DifferentStatements_NotAggregatedTogether`.
  - `CaptureDisabled_NoSubscription`.
  - `After_OutsideAmbientFlow_StillCompletesPendingStep`.
  - `After_WithEmptyOperationId_Ignored`.
  - `ChronicleSQLIntegrationTests.Query_InsideRoot_AppearsAsSQLStep` (real SqlClient 7.0.1 against MSSQL; settles crux B3-1).
- Premortem mitigations (step 00, 2026-11-02):
  - CRM-1: every `ChronicleRoot` carries its owning recorder (internal `Owner`, set by
    `ChronicleRecorder`). The observer records only into roots owned by its own host's recorder,
    so N hosts in one process record each command once. `StopAsync` and `Dispose` unsubscribe from
    `DiagnosticListener.AllListeners` and from every attached listener.
  - CRM-15: `OnNext` re-checks the gate, because `DiagnosticListener.IsEnabled(name)` is true when
    **any** subscriber enables the event and `Write` reaches every observer. `Before` records only
    inside an open root owned by this recorder. `After`/`Error` without a matching pending entry
    are ignored. The pending counter is decremented on every completion path.
  - CRM-10: SQL leaves start no `Activity` (SqlClient and EF instrumentation already emit spans).
  - CRM-11: with `CaptureSQLCommandText = false`, a failed SQL step records the exception type
    only (`Fail(exceptionType)`); the message is recorded only when capture is on, cut to
    `MaxAttributeValueLength`. SqlClient error messages can echo literal values.
  - Tests: `TwoSubscriptions_OneCommand_RecordedOnce` (two `ServiceProvider`s, both hosted services
    started, one synthetic command → one leaf), `Subscription_Stopped_ObserverReceivesNoEvents`,
    `OtherSubscriberEnabled_BeforeOutsideRoot_RecordsNothing` (a second always-true subscriber),
    `SQLStep_WithOperationsListener_StartsNoActivity`, and
    `Error_CaptureCommandTextOff_MessageNotRecorded`.
  - The integration test also records, in this step's `Deviations`, whether the SQL step duration
    includes reader row streaming (input for CRM-11 in step 15).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [ ] `dotnet test tests/SolTechnology.Core.SQL.Tests --filter "FullyQualifiedName~ChronicleSQL"` is green with Docker available.
- [ ] `grep -n "ProjectReference" src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj` prints nothing.
- [ ] `git diff --stat -- src/SolTechnology.Core.SQL` for this step prints nothing.
- [ ] If the integration test shows that SqlClient 7.0.1 raises no events, this step is set `blocked` and alternative B3 (b) goes back to the user. This is a pass/fail gate, not a silent fallback.
- [ ] CRM-1: `TwoSubscriptions_OneCommand_RecordedOnce` and `Subscription_Stopped_ObserverReceivesNoEvents` are green.
- [ ] CRM-15: `OtherSubscriberEnabled_BeforeOutsideRoot_RecordsNothing` is green.
- [ ] CRM-10: `SQLStep_WithOperationsListener_StartsNoActivity` is green.
- [ ] CRM-11: `Error_CaptureCommandTextOff_MessageNotRecorded` is green; the reader-duration finding is recorded under `Deviations`.

## Open questions
- none (OQ-1 confirmed by the user 2026-11-02: `CH-04` `CaptureSQLCommands`/`CaptureSQLCommandText`)

## Deviations
