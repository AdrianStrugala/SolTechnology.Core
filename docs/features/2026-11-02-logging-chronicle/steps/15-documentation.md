---
spec: 2026-11-02-logging-chronicle
step: 15
status: done
---

# Step 15: Documentation — `docs/Log.md` Chronicle section and module integration notes

## Summary
This step documents the shipped Chronicle behavior for consumers. `docs/Log.md` gets a full
`Chronicle` section: the concept, API, a real sample report, options, levels and sampling, N+1
hints, the flight recorder, OpenTelemetry mapping, EventIds 2154/2155, and the migration from
`ITimingService`. Each integrated module doc gets a short "Chronicle" subsection that says what
appears in the report and what is never recorded. It runs after the 2.0 module-doc rewrites, so
it adds sections rather than rewriting pages. Architecture pages are not touched here; the
retrospective (step 17) updates them.

## Affected components
- `docs/Log.md` — EDIT — NEW `### Chronicle` section; update "What ships in DI", "OpenTelemetry", "Operation lifecycle events", the EventId table, the request-envelope sample entry, and the "Features" bullets; remove the `ITimingService` section if step 09 left anything
- `docs/CQRS.md` — EDIT — `#### Chronicle` subsection
- `docs/Tale.md` — EDIT — `#### Chronicle` subsection; the new Logging package dependency (CP-01)
- `docs/Clients.md` — EDIT — `#### Chronicle` subsection
- `docs/SQL.md` — EDIT — `#### Chronicle` subsection
- `docs/Bus.md` — EDIT — `#### Chronicle` subsection
- `docs/Hangfire.md` — EDIT — `#### Chronicle` subsection

## Changes
- `docs/Log.md` `### Chronicle`:
  - Concept: one bounded tree of steps per root, one entry per root.
  - API: `IChronicle.Begin`, `BeginRoot`, `Step`, `Current`, `IsActive`; `ChronicleStep.Set`, `SetMasked`, `Fail`, `Cancel`; `using var step = chronicle.Step("ImportCities");`.
  - Sample report: the golden string of `Render_NestedTree_MatchesGolden` (step 03), copied verbatim.
  - Options table: every `ChronicleOptions` and `ChronicleFlightRecorderOptions` property with default, range, and `appsettings` path `Logging:Core:Chronicle`.
  - `AddSolChronicle(IConfiguration)` or `AddSolLogging(IConfiguration)` is needed for `appsettings` binding; the parameterless calls made by other installers use defaults.
  - Levels, tail sampling (failures, slow, and N+1 always kept), N+1 hint, truncation markers.
  - EventIds table: 2154 `ChronicleReported`, 2155 `RequestChronicleReported`, the unchanged 2137–2140, the request envelope 2141–2148 (2144–2146 only with `Enabled = false`; 2147 removed), and pointers to CQRS 2149 and Hangfire 2150–2153.
  - KQL example filtering on `ChronicleStatus` and `ElapsedMs`; `Chronicle` is tree text in OTel and App Insights (B6).
  - Silencing 2137–2139 through the `SolTechnology.Core.CQRS.PipelineBehaviors.LoggingPipelineBehavior` category level.
  - Flight recorder: opt-in, failed roots only, Serilog needs `writeToProviders: true` or `builder.Logging.AddSerilog()`; with `writeToProviders`, Serilog's `MinimumLevel` must admit Debug.
  - Serilog: `{@Chronicle}` is destructured as a structure (not the ASCII tree) and capped by `MaximumDestructuringDepth` (default 10, below `MaxDepth` 12).
  - SQL capture runs in a hosted service, so it is active only in hosts that start `IHostedService`s.
  - Console `SingleLine: true` flattens the tree into one line; recommend `SingleLine: false` where humans read the console.
  - OpenTelemetry: steps are Activities on `CoreLoggingActivitySources.OperationsName`; `EmitActivities = false` opts out; the CQRS span is now the Chronicle step span.
  - `#### Migrating from ITimingService (1.x)`: the brief's release-notes bullets. This subsection is the only place in `docs/` outside `docs/features/` that names `ITimingService`.
  - `BeginRoot` for work that outlives its caller.
- `docs/CQRS.md`: requests as steps or roots; event-handler steps; fire-and-forget events as their own roots (CP-06); `UseLogging = false` registers no Chronicle.
- `docs/Tale.md`: `Tale {Handler}` and `Chapter {ChapterId}` steps; outcome mapping; interactive input never recorded.
- `docs/Clients.md`: `HTTP {Method} {client}` steps, `Attempts`, `StatusCode`, `Host`; path and query never recorded; 4xx is `Ok`, 5xx is `Failed`.
- `docs/SQL.md`: SqlClient commands captured from `SqlClientDiagnosticListener` for Dapper, EF Core, and raw `SqlConnection`; `CaptureSQLCommandText` is off by default because statements can contain PII; other providers are not captured.
- `docs/SQL.md` also: connection-open retries (up to about 40 seconds) are not SQL steps and show as a gap in the parent step.
- `docs/Bus.md`: one root per message; settlement → status mapping (completed, complete failed, abandoned, dead-lettered with its reason code, shutdown); body never recorded.
- `docs/Hangfire.md`: one root per job; `UseSolFilters(sp)` is required; recurring-job and persistent-event names; a failed `Result` (recoverable or not) is a `Failed` report although Hangfire shows `Succeeded` or a retry; a run skipped by the overlap guard has no report (it shows as `Deleted`); a dispatch stopped by shutdown is `Cancelled` and its rerun has its own report.
- Premortem mitigations (step 00, 2026-11-02):
  - CRM-3 (`docs/Log.md`): the 32,768-char report budget and the `+{n} lines truncated` marker;
    with OTel `IncludeFormattedMessage = true`, the tree is exported as both the body and the
    `Chronicle` attribute.
  - CRM-4 (`docs/Log.md`): reports that contain a failed step are never sampled away; with
    `SampleRate < 1`, log-count request-rate and error-ratio queries are invalid, so use ASP.NET
    Core request metrics instead.
  - CRM-7 (`docs/Log.md`): a `Log contract changes in 2.0` table. It maps each 1.x entry
    ("Finished request" with and without `{@Timings}`, "failed after", "aborted by client":
    template, level, EventId) to its 2.0 replacement, with a before/after KQL pair, the
    slow-request Warning, and the `SlowThreshold` knob. Re-check at implementation: the 2.0
    logging work gave these entries EventIds 2146, 2147, 2145, and 2144 (1.x logged them without
    an EventId), so the table has a 1.x column, a "2.0, Chronicle disabled" column, and a
    "2.0, Chronicle enabled" (2155) column.
  - CRM-8 (`docs/Log.md`): flight-recorder worst-case memory (`MaxEntries × MaxMessageLength ×
    2 bytes × concurrent roots`); inside roots, `IsEnabled(Debug)`-guarded code runs; a Serilog
    example with per-sink `restrictedToMinimumLevel: Information`.
  - CRM-9 (`docs/Log.md`): a root must not span a service lifetime; open one root per loop
    iteration or job.
  - CRM-10 (`docs/Log.md`): step names must be constant (no interpolated IDs; put IDs in
    attributes), because they become span names and aggregation keys.
  - CRM-11 (`docs/SQL.md`, `docs/Log.md`): with `CaptureSQLCommandText = false`, SQL error
    messages are not recorded; what the SQL step duration covers (from step 07's `Deviations`);
    `Set` records raw values and `SetMasked` is required for PII.
  - CRM-12 (`docs/Log.md` migration subsection): the removal of `ITimingService` is
    binary-breaking. Rebuild every assembly compiled against Logging 1.x that references it;
    `SOLCORE001` does not detect this, and `ValidateOnBuild` surfaces it at startup.

## Acceptance criteria
- [ ] `grep -c "^### Chronicle" docs/Log.md` prints `1`.
- [ ] `grep -rln "ITimingService" docs --include=*.md | grep -v "docs/features/\|docs/architecture/"` prints only `docs/Log.md`, and every match there lies under `#### Migrating from ITimingService (1.x)`. `docs/architecture/logging.md` still describes `ITimingService` until step 17 updates it.
- [ ] `grep -l "Chronicle" docs/CQRS.md docs/Tale.md docs/Clients.md docs/SQL.md docs/Bus.md docs/Hangfire.md | wc -l` prints `6`.
- [ ] The sample tree in `docs/Log.md` is byte-identical to the golden string in `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleTreeRendererTests.cs`.
- [ ] `git grep -niE "decompil|ilspy|dotpeek" -- docs ':!docs/features/2026-11-02-logging-chronicle*'` prints nothing (clean room).
- [ ] The `documentation-cleanup` skill reports no broken links or tables in the edited files.
- [ ] CRM-3, CRM-4, CRM-7, CRM-8, CRM-9, CRM-10, CRM-11, CRM-12: each bullet above is present in the named doc (`grep -n "lines truncated\|Log contract changes in 2.0\|restrictedToMinimumLevel\|binary-breaking" docs/Log.md` lists four matches; `grep -n "CaptureSQLCommandText" docs/SQL.md` mentions error messages).

## Open questions
- none

## Deviations

### Serilog stores the report as text

**Original plan:** `docs/Log.md` says Serilog destructures `{@Chronicle}` as a structure capped
by `MaximumDestructuringDepth`.

**Actual implementation:** step 04 found that the state key is `Chronicle`, without `@`, so
Serilog binds the report without destructuring and stores the ASCII tree as a string, like
OpenTelemetry and Application Insights. `docs/Log.md` ("Log entries") documents the string form
and recommends filtering on the scalar properties; the brief's release-notes bullet is corrected
the same way.

### Placement

- `### Chronicle` sits before `### Testing` in `docs/Log.md`, with `####` subsections (API, what
  becomes a root or a step, options, levels and sampling, reading a report, log entries, log
  contract changes, flight recorder, rules of use, migration). It is a top-level section outside the
  canonical structure of the Public Documentation Guide, as this step requires (`grep "^### Chronicle"`).
- Each module page has `#### Chronicle` under `### Usage`; `docs/Log.md` links to them by anchor.
- `docs/Log.md` also updates the Features bullets, the OpenTelemetry subsection (the CQRS span is the
  step span), the App Insights sample row, the production levels, and "What ships in DI".
- `docs/architecture/` is not touched (R9); its `ITimingService` text is step 17's.

### CRM-7 re-check at implementation (2026-11-10)

Holds. "Log contract changes in 2.0" has three columns (1.x without EventId; 2.0 with Chronicle
disabled: 2146, 2147 retired, 2145, 2144; 2.0 with Chronicle enabled: 2155), a before/after KQL
pair, the slow-request Warning with `SlowThreshold`, and the 5xx-without-exception row of CRM-5.

