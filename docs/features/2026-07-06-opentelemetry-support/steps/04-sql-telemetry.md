---
spec: 2026-07-06-opentelemetry-support
step: 04
status: to-do
---

# Step 04: SQL telemetry

## Summary

Adds the SQL module's own spans and counters where vendor instrumentation has no
coverage: unit-of-work lifetime and connection-open retries. Command-level spans stay
vendor-side (Npgsql ships an `ActivitySource`; Microsoft.Data.SqlClient support is
verified here, not assumed).

## Affected components

- `src/SolTechnology.Core.SQL/Telemetry/SqlTelemetry.cs` — NEW — source + meter
- `src/SolTechnology.Core.SQL/Transactions/UnitOfWork.cs` — EDIT — unit-of-work span
- `src/SolTechnology.Core.SQL/Connections/SQLConnectionFactory.cs` — EDIT — retry counter
- `src/SolTechnology.Core.SQL/ModuleInstaller.cs` — EDIT — `TryAddSingleton`
- `src/SolTechnology.Core.SQL/SolTechnology.Core.SQL.csproj` — EDIT — minor version bump
- `src/SolTechnology.Core.Logging/Telemetry/TelemetryDefaults.cs` — EDIT (conditional) — vendor source literal
- `docs/SQL.md` — EDIT (conditional) — consumer opt-in recipe
- `tests/SolTechnology.Core.SQL.Tests` — EDIT — span + metric tests

## Changes

- Precondition task: determine whether the pinned `Microsoft.Data.SqlClient` ships a
  native `ActivitySource` and its exact name literal (do not assume from memory).
  - Ships one → add the literal as a constant in `TelemetryDefaults` (Logging package,
    same PR) so `AddSolTelemetry` subscribes it; note in `docs/SQL.md`.
  - Does not → document the consumer opt-in (`OpenTelemetry.Instrumentation.SqlClient`)
    in `docs/SQL.md`; do NOT add that package to any `src/` project.
  - Npgsql: its built-in source name `Npgsql` is documented in `docs/SQL.md`
    (subscribable via `TelemetryOptions.AdditionalSources`; not added to the default
    wildcard set — SQL Server consumers should not subscribe Npgsql noise by default).
- NEW `SqlTelemetry` (stable contract — MAJOR bump to change):
  - `ActivitySource` name `SolTechnology.Core.SQL`.
  - `Meter` name `SolTechnology.Core.SQL` via `IMeterFactory`.
  - `Counter<long> soltechnology.core.sql.connection_open_retries` — tag `attempt`.
- EDIT `UnitOfWork`: span `sql.unit_of_work` from `Begin` to completion; terminal tag
  `outcome` = `committed` | `rolled_back` (dispose-without-complete counts as
  `rolled_back`); `SetStatus(Error)` + `AddException` when completion throws.
- EDIT `SQLConnectionFactory`: increment `connection_open_retries` from the existing
  retry callback.
- EDIT `ModuleInstaller`: `services.TryAddSingleton<SqlTelemetry>()`.
- csproj: minor version bump.
- Tests: `ActivityListener` — commit path tags `outcome=committed`, dispose-only path
  tags `outcome=rolled_back` (`[TestCase]` if assert shape is identical);
  `MetricCollector<long>` for the retry counter.

## Acceptance criteria

- [ ] `dotnet build SolTechnology.Core.slnx` green.
- [ ] `dotnet test tests/SolTechnology.Core.SQL.Tests` green.
- [ ] SqlClient `ActivitySource` question resolved and recorded (TelemetryDefaults
      constant or `docs/SQL.md` opt-in recipe — exactly one of the two).

## Open questions

- none

## Deviations

<!-- Empty at authoring time. Filled by implement-plan when reality diverges from the plan. -->
