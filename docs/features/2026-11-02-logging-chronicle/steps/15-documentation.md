---
spec: 2026-11-02-logging-chronicle
step: 15
status: to-do
---

# Step 15: Documentation — `docs/Log.md` Chronicle section and module integration notes

## Summary
This step documents the shipped Chronicle behavior for consumers. `docs/Log.md` gets a full
`Chronicle` section: the concept, API, a real sample report, options, levels and sampling, N+1
hints, the flight recorder, OpenTelemetry mapping, EventIds 2150/2151, and the migration from
`ITimingService`. Each integrated module doc gets a short "Chronicle" subsection that says what
appears in the report and what is never recorded. It runs after the 2.0 module-doc rewrites, so
it adds sections rather than rewriting pages. Architecture pages are not touched here; the
retrospective (step 17) updates them.

## Affected components
- `docs/Log.md` — EDIT — NEW `### Chronicle` section; update "What ships in DI", "OpenTelemetry", and "Operation lifecycle events"
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
  - EventIds table: 2150 `ChronicleReported`, 2151 `RequestChronicleReported`, and the unchanged 2137–2140.
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
- `docs/Bus.md`: one root per message; settlement → status mapping; body never recorded.
- `docs/Hangfire.md`: one root per job; `UseSolFilters(sp)` is required; recurring-job and persistent-event names.

## Acceptance criteria
- [ ] `grep -c "^### Chronicle" docs/Log.md` prints `1`.
- [ ] `grep -rln "ITimingService" docs --include=*.md | grep -v "docs/features/"` prints only `docs/Log.md`, and every match there lies under `#### Migrating from ITimingService (1.x)`.
- [ ] `grep -l "Chronicle" docs/CQRS.md docs/Tale.md docs/Clients.md docs/SQL.md docs/Bus.md docs/Hangfire.md | wc -l` prints `6`.
- [ ] The sample tree in `docs/Log.md` is byte-identical to the golden string in `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleTreeRendererTests.cs`.
- [ ] `git grep -niE "decompil|ilspy|dotpeek" -- docs ':!docs/features/2026-11-02-logging-chronicle*'` prints nothing (clean room).
- [ ] The `documentation-cleanup` skill reports no broken links or tables in the edited files.

## Open questions
- none

## Deviations
