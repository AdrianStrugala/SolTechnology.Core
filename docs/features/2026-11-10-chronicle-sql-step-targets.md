---
status: completed
created: 2026-11-10
completed: 2026-11-10
---

# Chronicle — name SQL steps after their target table

> Historical delivery record. It may not describe the current system.

## Goal

Chronicle SQL steps in `SolTechnology.Core.Logging` say which table or procedure a command touched:
`SQL {OPERATION} {target}` instead of `SQL {keyword}`, so a slow, failed or N+1 line in a report
points at its table without turning on `CaptureSQLCommandText`.

## Context

**Requested and approved by:** the owner on 2026-11-10, with the design below. It follows
[`2026-11-02-logging-chronicle`](2026-11-02-logging-chronicle.md), which shipped SQL steps named
after the first keyword. Chronicle is unreleased 2.0 behaviour, so the new names, including
`SQL EXEC {procedure}` for stored procedures, need no migration.

**Observed in DreamTravel before the change:**

| Command | Step name |
|---|---|
| EF Core `SELECT [c2].[Id], … FROM (SELECT … FROM [Cities] AS [c] …) AS [c2] …` | `SQL SELECT` |
| Hangfire `set xact_abort on; set nocount on; declare @jobId bigint; begin tran; insert into [HangFire].Job …` | `SQL DECLARE` |
| Hangfire `select top (1) Value from [HangFire].JobParameter with (forceseek) where …` | `SQL SELECT` |
| Hangfire `insert into [HangFire].JobQueue (JobId, Queue) values (…)` | `SQL INSERT` |

**Current state (`chronicle-sql-targets@b884e8e4`):** `SQLClientPayloadReader.StepName` built the
name; `ChronicleSQLClientObserver` grouped steps by `{name}#{statement hash}`.

**Gates.** No public or protected symbol changes, no `ModuleInstaller.cs`, no options, no package.
No `CLAUDE.md §2` confirmation or premortem trigger applies. Single-session change per
[delivery workflow](../architecture/delivery-workflow.md): no working folder.

## Design decisions (approved 2026-11-10)

1. **Name format.** `SQL {OPERATION} {target}`, after the OpenTelemetry database span name
   `{db.operation.name} {db.collection.name}`. The operation is upper-case. The target loses
   brackets, double quotes and backticks and keeps at most its last two name parts
   (`[db].[HangFire].[Job]` → `HangFire.Job`, `[Cities]` → `Cities`). No target gives
   `SQL {OPERATION}`; nothing at all gives `SQL`.
2. **Leading noise skipped.** Whitespace, `--` and `/* */` comments, `SET …;`, `DECLARE …;`, and
   `BEGIN TRAN` / `BEGIN TRANSACTION` with an optional `;`.
3. **Target per operation.** `SELECT`: the first `FROM` followed by a name, looking into a
   `FROM (` subquery; no `FROM` gives `SQL SELECT`. `INSERT` and `MERGE`: after an optional `INTO`.
   `UPDATE`: the name after it. `DELETE`: after an optional `FROM`. `EXEC` / `EXECUTE`: the
   procedure, skipping `@ret =`. A `CommandType.StoredProcedure` command: `SQL EXEC {procedure}`.
   `TRUNCATE TABLE x`: `SQL TRUNCATE x`. Other operations: the operation only.
4. **Not a target.** Table variables (`@t`). Temp tables (`#t`) are targets. Keywords inside
   string literals, comments or bracketed names never match; a small tokenizer, not a parser.
5. **Bounds.** At most the first 4096 characters are scanned; a target over 128 characters is
   dropped; the name still goes through `MaxAttributeValueLength`. Span-based, no `Regex`, no
   allocation except the name, and only after the observer's open-root gate.
6. **Privacy.** Only identifiers reach the name, never literals or values. `CaptureSQLCommandText`
   stays `false` by default and is unchanged.
7. **Aggregation and N+1.** The group key stays `{name}#{statement hash}`. The name depends only on
   the statement, so the same statement still aggregates into one line, now with its table, and
   different statements on one table stay separate lines.

## Scope

### In scope

- `src/SolTechnology.Core.Logging/Chronicles/SQL`: new internal `SQLStepNamer` and
  `SQLTokenizer`; `SQLClientPayloadReader` keeps only payload reading; the observer calls the namer.
- Tests in `SolTechnology.Core.Logging.Tests`, the SqlClient integration test in
  `SolTechnology.Core.SQL.Tests`, and the DreamTravel Chronicle component test.
- `docs/Log.md`, `docs/SQL.md`, `docs/architecture/logging.md`,
  `docs/architecture/infrastructure-modules.md`.

### Out of scope

- The DreamTravel README sample report (being rewritten on master; the owner refreshes it).
- Full SQL parsing: `UPDATE TOP (n)`, `DELETE TOP (n)` and CTE bodies are not looked into
  (`WITH …` gives `SQL WITH`).

## Acceptance criteria

- [x] The four example commands give `SQL SELECT Cities`, `SQL INSERT HangFire.Job`,
  `SQL SELECT HangFire.JobParameter` and `SQL INSERT HangFire.JobQueue` (the DreamTravel table
  itself is mapped as `City`, so the live report shows `SQL SELECT City`).
- [x] One parameterized test covers every design case: the four examples, `UPDATE`, `DELETE`,
  `MERGE`, `EXEC @r =`, stored procedure, `TRUNCATE`, `SELECT 1`, table variable, temp table,
  `FROM fake` in a comment, a string literal and a bracketed name, three-part and quoted names,
  128/129-character targets, the 4096-character bound, empty, whitespace and `null` text.
- [x] A test shows that naming allocates only the name.
- [x] Aggregated and N+1 lines show the specific name; different statements on one table stay apart.
- [x] Every new or changed file is at most 150 lines.
- [x] Release build has no new warnings; Logging, SQL Chronicle, Hangfire and DreamTravel
  Chronicle tests are green.

## Completion summary

Delivered on 2026-11-10 in one session, as designed.

- **Code.** `SQLTokenizer` (a `ref struct`) returns names (multi-part, quoted with `[]`, `""` or
  backticks, with doubled-quote escapes), string literals and single symbols, and skips whitespace
  and comments. `SQLStepNamer` skips the preamble, finds the target per operation, and writes the
  name into a stack buffer, so the only allocation is the final string. The observer passes the
  command text and type; `SQLClientPayloadReader` lost its naming code.
- **Tests.** `SQLStepNamerTests` (31 cases plus the allocation test) replaced
  `SQLClientPayloadReaderTests`. `ChronicleSQLClientObserverTests` pins `SQL SELECT dbo.Cities`,
  `SQL EXEC dbo.GetCities`, the N+1 line `SQL SELECT dbo.Streets x42`, and two statements on one
  table that share a name but not a line. `ChronicleSQLIntegrationTests` against SQL Server
  expects `SQL SELECT dbo.Items` and `SQL SELECT sys.objects`, and now checks that the statement
  text (`COUNT(*)`) stays out of the report instead of the table name. The DreamTravel
  `FindCityChronicleTests` expects `SQL SELECT City`.
- **Verification.** Release build of `SolTechnology.Core.slnx`: 0 errors, no warning in a changed
  file. Logging 282/282, SQL Chronicle integration 2/2, Hangfire 97/97, DreamTravel Chronicle
  component 3/3. The DreamTravel run showed `SQL SELECT City`, `SQL INSERT HangFire.Job`,
  `SQL SELECT HangFire.JobParameter`, `SQL INSERT HangFire.JobQueue` and
  `SQL SELECT HangFire.Schema`.

## Deviations

- A leading `;` (as in `;WITH …`) is skipped like the rest of the preamble.
- The operation must be a whole word of ASCII letters of at most 32 characters; otherwise the name
  is `SQL`. Before, the leading letters of any word were used (`sp_who` gave `SQL SP`).
- `SELECT` scanning stops at the first `;`, so a later statement in a batch cannot supply the target.
