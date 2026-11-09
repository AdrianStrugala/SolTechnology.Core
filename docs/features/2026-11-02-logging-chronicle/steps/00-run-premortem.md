---
spec: 2026-11-02-logging-chronicle
step: 00
status: done
---

# Step 00: Run premortem (gate)

## Summary
A premortem is mandatory (`CLAUDE.md §4`): steps 04, 06, 07, and 09 change the Logging
registrations reached from `ModuleInstaller.cs`, and steps 10–14 edit the `ModuleInstaller.cs` of
CQRS, Tale, HTTP, MessageBus, and Hangfire. Run the
[premortem skill](../../../../.github/skills/premortem/SKILL.md) in a session that did not author
this plan, after `review` is `done` and after the cross-feature dependencies of steps 01–04 are
`done` (see the brief), so the evidence is current. Record the full output here and the verdict in
`summary.md` `premortem:`. This step changes no `src/` code.

## Affected components
- `docs/features/2026-11-02-logging-chronicle/steps/00-run-premortem.md` — EDIT — append a `## Premortem` section (before `## Deviations`) with the full skill output
- `docs/features/2026-11-02-logging-chronicle/summary.md` — EDIT — `premortem:` verdict
- Step files named by required mitigations — EDIT (by the executing `implement-plan` session)

## Changes
- **Modules touched:**
  - `SolTechnology.Core.Logging`: new `Chronicles/` folder, `LoggingMiddleware`, `RequestEnvelopeLogger`, `LoggingServiceRegistrations`, `ModuleInstaller`; `ITimingService`/`TimingService` deleted (steps 01–09).
  - `SolTechnology.Core.CQRS`: `LoggingPipelineBehavior`, `EventDispatcher`, `InMemoryEventPublisher`, `ModuleInstaller` (step 10).
  - `SolTechnology.Core.Tale`: csproj (new Logging edge), `TaleHandler`, `TaleEngine`, `TaleStepInterpreter`, `ModuleInstaller` (step 11).
  - `SolTechnology.Core.HTTP`: two new internal handlers, `ModuleInstaller` (step 12).
  - `SolTechnology.Core.MessageBus`: `MessageBusReceiver`, `ModuleInstaller` (step 13).
  - `SolTechnology.Core.Hangfire`: new `ChronicleJobFilter`, `ModuleInstaller` (step 14).
  - `SolTechnology.Core.SQL`: no source change; SqlClient commands are observed from Logging (step 07).
  - DreamTravel: `FindCityByNameHandler` (step 09); API and Worker `Program.cs`, `appsettings*.json`, README, component tests (step 16).
  - Module docs (step 15).
- **Contract delta:** see the feature brief
  [`../../2026-11-02-logging-chronicle.md`](../../2026-11-02-logging-chronicle.md):
  - Public API: `CH-01`…`CH-11` (new Chronicle API, deleted `ITimingService`, changed `LoggingMiddleware.InvokeAsync` and `LoggingPipelineBehavior` constructor).
  - Package and behavior: `CP-01`…`CP-06` (Tale → Logging edge, more `OperationsName` spans, `AddSolChronicle` from six installers, SqlClient diagnostic subscription, flight-recorder provider, fire-and-forget event roots).
  - Log contract: `LC-01`…`LC-05` (EventId 2155 replaces the finish entry, one Error entry per failed request, new EventId 2154, sampling, flight-recorder records).
  - No new external `PackageReference` in `src/`; test projects gain `Microsoft.Extensions.Diagnostics.Testing`.
  - Semver: major (part of 2.0).
- **`ModuleInstaller.cs` edits:**
  - `src/SolTechnology.Core.Logging/ModuleInstaller.cs`: steps 04 (direct), 06, 07, 09 (through the registration files it calls).
  - `src/SolTechnology.Core.CQRS/ModuleInstaller.cs`: step 10.
  - `src/SolTechnology.Core.Tale/ModuleInstaller.cs`: step 11.
  - `src/SolTechnology.Core.HTTP/ModuleInstaller.cs`: step 12.
  - `src/SolTechnology.Core.MessageBus/ModuleInstaller.cs`: step 13.
  - `src/SolTechnology.Core.Hangfire/ModuleInstaller.cs`: step 14.
- **`Directory.Build.props` edits:** none.
- **Persisted contracts:** none. Hangfire job arguments and parameters are read, never written (step 14).
- **Premortem focus named by the plan:** the log contract changes `LC-01`…`LC-05`, the package edge `CP-01`, the new roots `CP-06`, the SqlClient reflection crux (B3), and `AsyncLocal` behavior across Hangfire filters and `Task.Run` (B11).
- **Applicable checklists** (`.github/skills/premortem/references/`): `logging.md`, `di.md`, `cqrs.md`, `story.md` (Tale), `http.md`, `sql.md`, `messagebus.md`, `hangfire.md`, `build-and-nuget.md`, `testing-kits.md`.
- **Steps under review:**
  - Core: [01](01-chronicle-core-step-tree.md),
    [02](02-bounded-recording-and-options-validation.md),
    [03](03-report-model-renderer-and-n-plus-one.md),
    [04](04-emission-level-sampling-and-registration.md),
    [05](05-opentelemetry-activities.md),
    [06](06-flight-recorder.md),
    [07](07-sqlclient-diagnostic-steps.md),
    [08](08-detached-roots.md).
  - Roots and removal: [09](09-http-request-roots-and-timing-service-removal.md).
  - Integrations: [10](10-cqrs-auto-steps.md),
    [11](11-tale-auto-steps.md),
    [12](12-http-client-auto-steps.md),
    [13](13-messagebus-message-roots.md),
    [14](14-hangfire-job-roots.md).
  - Documentation and showcase: [15](15-documentation.md),
    [16](16-dreamtravel-showcase.md).
  - Close: [17](17-retrospective.md).

## Acceptance criteria
- [x] Premortem output (frame, imagined failure, scenario table, top 3 risks, required mitigations naming their step files, accepted risks, decision) recorded in this file.
- [x] `summary.md` `premortem:` updated to the verdict in the same change.

## Open questions
- none

## Premortem

Run 2026-11-02 in a session that did not author the plan (`implement-plan` §5, premortem skill).
Evidence base: `master@ae2e1ae3` source plus the plan text for the 2.0 per-library changes that
have not landed yet (see `Deviations`). Clean room: no `~/.nuget/packages/aiia*` path and no other
repository was opened.

### Premortem — `Chronicle — per-operation step reports in SolTechnology.Core.Logging (steps 01–17)`

#### Frame

- **Modules touched**: `SolTechnology.Core.Logging` (new `Chronicles/`, `LoggingMiddleware`,
  `RequestEnvelopeLogger`, `LoggingServiceRegistrations`, `ModuleInstaller`; `ITimingService` and
  `TimingService` deleted), `SolTechnology.Core.CQRS` (`LoggingPipelineBehavior`,
  `EventDispatcher`, `InMemoryEventPublisher`, `ModuleInstaller`), `SolTechnology.Core.Tale`
  (csproj edge CP-01, `TaleHandler`, `TaleEngine`/`TaleStepInterpreter`, `ModuleInstaller`),
  `SolTechnology.Core.HTTP` (two internal handlers, `ModuleInstaller`),
  `SolTechnology.Core.MessageBus` (`MessageBusReceiver`, `ModuleInstaller`),
  `SolTechnology.Core.Hangfire` (`ChronicleJobFilter`, `ModuleInstaller`).
  `SolTechnology.Core.SQL` has no source change; it is observed process-wide through SqlClient's
  `DiagnosticListener` (step 07). DreamTravel (steps 09, 16) and module docs (step 15).
- **API delta**: added `IChronicle` (+ `BeginRoot`), `ChronicleStep`, `ChronicleStepStatus`,
  `ChronicleOptions`, `ChronicleFlightRecorderOptions`, `ChronicleReport`,
  `ChronicleReportStep`, `ChronicleFlightRecord`, two `AddSolChronicle` overloads (CH-01…07,
  CH-11). Removed `ITimingService` (CH-08, `src/SolTechnology.Core.Logging/ITimingService.cs:15-33`).
  Changed `LoggingMiddleware.InvokeAsync` last parameter (CH-09,
  `Middleware/LoggingMiddleware.cs:31-37`) and the `LoggingPipelineBehavior` constructor (CH-10,
  `PipelineBehaviors/LoggingPipelineBehavior.cs:18-21`). Log contract LC-01…05 (EventIds 2154 and
  2155 are new; today only 2137–2140 exist, `OperationLogMessages.cs:18,25,32,50`). Behaviour
  CP-01…06. All confirmed by the user on 2026-11-02 (OQ-1, OQ-2).
- **Semver impact**: **MAJOR**. CH-08 deletes a public interface; CH-09 and CH-10 change public
  signatures; LC-01/LC-02 replace the request finish entries (`LoggingMiddleware.cs:106-147`).
  It ships inside 2.0.0 (flagship "Bump every package to 2.0.0"), so the classification matches the
  diff.
- **Consumers in workspace**: DreamTravel —
  `DreamTravel.Queries/FindCityByName/FindCityByNameHandler.cs:14,22-30` (`ITimingService`);
  `DreamTravel.Api/Program.cs:45-46` (`OperationsName` tracing), `:116` `AddSolLogging()`,
  `:124` `AddSolCQRS()`, `:174` `UseDeveloperExceptionPage` (outer), `:186` `UseSolLogging()`;
  `DreamTravel.ServiceDefaults/Extensions.cs:46-49` (OTel logging with
  `IncludeFormattedMessage = true`); `DreamTravel.Sql/ModuleInstaller.cs:14-25` (EF Core
  `UseSqlServer` and Hangfire `UseSqlServerStorage`); `DreamTravel.Api/appsettings.json`
  (`Console` `SingleLine = true`); `tests/Component/ComponentTestsFixture.cs:44-45` (the Worker
  and API hosts run **in one process**). External: invisible public NuGet consumers of every
  touched package, including users of `SolTechnology.Core.Api`'s `ExceptionFilter`.

#### Imagined Failure

Three weeks after 2.0.0 ships, a public consumer files: "since upgrading, every request writes a
multi-kilobyte log entry. Our App Insights bill doubled, the trees are cut off exactly where the
failure was, the error-ratio alert fires on slow 200s, and our `ChronicleStatus == "Failed"`
query never matches our 500s." Their integration-test suite, which spins up several
`WebApplicationFactory` hosts, reports every SQL statement twice and flags `N+1 suspected` on
ordinary pages. Operations set `Logging:Core:Chronicle:Enabled = false` during the incident and
nothing changes, because `AddSolCQRS()` had already registered Chronicle with defaults. On the
Worker, a Hangfire job whose later filter threw left its root ambient on a dedicated worker thread,
and Hangfire's own storage queries pile into a report that is never emitted. Separately, a
`Task.WhenAll` branch that called `chronicle.Current.Set(...)` threw `IndexOutOfRangeException`
out of the instrumentation into business code, turning a healthy request into a 500 and a Service
Bus message into an abandon loop.

#### Scenarios

| # | Story (what + mechanism) | Trigger (file:line) | Blast radius | Sev | Lik | Existing control | Mitigation → step |
|---|---|---|---|---|---|---|---|
| 1 | **Duplicate SQL capture across hosts in one process.** Each host's `ChronicleSQLClientSubscription` subscribes to the process-wide `DiagnosticListener.AllListeners`, while the ambient tree (`AmbientChronicle`, static `AsyncLocal`) is shared by every host. Every observer appends a leaf to the same ambient node, so with N hosts each command appears N times, counts double, and `N+1 suspected` fires early. Undisposed test hosts keep their subscriptions and service providers alive. | Step 07 `ChronicleSQLClientSubscription` + `StartDetachedLeaf`; CP-03 registers it from six installers; `DreamTravel/tests/Component/ComponentTestsFixture.cs:44-45` starts Worker and API hosts in one process | sample apps + public consumers' test suites (rarely prod) | M | H | none — gap (step 07 tests dispose per test; the integration test builds one provider) | CRM-1 → 07 (early warning: doubled `SQL` lines, spurious `N+1 suspected`) |
| 2 | **Hangfire worker-thread ambient leak.** `ChronicleJobFilter.OnPerforming` writes the ambient node synchronously on a dedicated Hangfire worker thread. If `OnPerformed` is skipped for it (a later filter's `OnPerforming` throws, or the perform aborts), the open job root stays ambient on that thread. Hangfire storage SQL and Hangfire Debug records then accumulate into a root that is never emitted. The next job's root carries a wrong `DetachedFrom`, and its `OnPerformed` restores the zombie root. | `src/SolTechnology.Core.Hangfire/Filters/CorrelationIdJobFilter.cs:30-47` writes an `AsyncLocal` in `OnPerforming` and never restores it (`:49-55`), which is the precedent the hangfire 2.0 feature fixes; step 14 `ChronicleJobFilter`; `DreamTravel.Sql/ModuleInstaller.cs:14-25` (`UseSqlServerStorage`) | sample apps + public Hangfire consumers | M | L | step 14 `OnPerformed_RestoresPreviousAmbientNode`, real-performer `JobBody_ChronicleSteps_NestUnderJobRoot` (happy path only) | CRM-2 → 14 |
| 3 | **Total report size is unbounded.** The caps are per dimension and multiply: 256 steps × 8 attributes × 200 chars ≈ 400 K chars, and step names have no cap at all (`Step(string name)`, SQL `{procedure name}`). The OTel logging provider with `IncludeFormattedMessage` exports the tree twice (body and `Chronicle` attribute). OTLP and App Insights size limits cut the tail, which is where the failure usually is, and ingestion cost spikes. | Brief defaults table (02); B6 `[{@Chronicle}]`; `DreamTravel.ServiceDefaults/Extensions.cs:46-49`; step 03 renderer has no total budget | all | M | M | per-dimension caps only (step 02) — gap | CRM-3 → 02, 03, 15 |
| 4 | **Sampling hides incidents behind an `Ok` root.** `EventDispatcher` isolates handler exceptions, so a fire-and-forget event root stays `Ok` with a `Failed` handler step. A swallowed HTTP 5xx or timeout (step 12) or a caught SQL error leaves a `Failed` step under an `Ok` request. Step 04 keeps only `Status != Ok`, level ≥ Warning, or N+1, so with `SampleRate < 1` these reports are dropped. Log-count error-ratio dashboards also skew by `1/SampleRate`, because failures are always kept and successes are sampled. | `src/SolTechnology.Core.CQRS/Internal/EventDispatcher.cs:22-30`; step 10 CP-06; step 12 outcome mapping; step 04 emission policy (LC-04) | public consumers that opt into sampling | M | M | default `SampleRate = 1.0`; per-handler Error log still written by `EventDispatcher` | CRM-4 → 03, 04, 15 |
| 5 | **Handled 5xx reports `ChronicleStatus = Ok`.** The Api package's `ExceptionFilter` maps an exception to a 5xx `ObjectResult` and sets `ExceptionHandled = true`, so `LoggingMiddleware` sees no exception. Step 09 only calls `Fail` on exceptions, so the 2155 entry is Error-level with `ChronicleStatus = Ok`. That is inconsistent with step 12 (`Fail("HTTP{status}")`), and `ChronicleStatus`-based KQL misses every mapped 500. | `src/SolTechnology.Core.Api/Filters/ExceptionFilter.cs:47-80`; `DreamTravel.Api/Program.cs:186`; step 09 "Status and level" | sample apps + every Api-package consumer | M | H | level is still Error (status-based, `LoggingMiddleware.cs:124` today) | CRM-5 → 09 |
| 6 | **The configuration kill switch is ignored.** `AddSolCQRS`, `AddSolTale`, `AddSolHTTPClient`, and others call parameterless `AddSolChronicle()` (CP-03). If the `IConfiguration` overload short-circuits on the "already registered" guard (step 04 test `AddSolChronicle_CalledTwice_RegistersOnce`), then `Enabled = false`, `SampleRate`, and `CaptureSQLCommandText` from `appsettings` are silently ignored. The result depends on call order. | Step 04 `ChronicleServiceRegistrations` (`TryAdd*`); step 16 adds `AddSolChronicle(builder.Configuration)` next to `DreamTravel.Api/Program.cs:116` while `:124` `AddSolCQRS()` also registers it; precedent `Logging/ModuleInstaller.cs:31-67` (the two overloads register different sets) | all | M | M | `AddSolLoggingConfiguration_BindsChronicleSection` (single call only) | CRM-6 → 04, 16 |
| 7 | **Consumer alerts and queries break on the new finish entry.** Today: "failed after" Error, "aborted by client" Warning, and "Finished request" with EventId 0 and an optional `{@Timings}`. 2.0: one 2155 entry, a slow 2xx logs Warning at 1 s, the `{OriginalFormat}` text changes, and `Timings` disappears. Warning-count alerts fire on slow successes; alerts matching the "failed after" message stop silently. | `src/SolTechnology.Core.Logging/Middleware/LoggingMiddleware.cs:106-147`; LC-01/LC-02; `docs/Log.md:214,293,309-316` | public consumers | M | M | user acceptance OQ-2 (LC-01…05); scalar placeholders kept | CRM-7 → 15 |
| 8 | **Flight-recorder cost.** Inside roots, `IsEnabled(Debug)` turns true for every non-`Microsoft`/`System` category, so every `LogDebug` call and every `if (IsEnabled(Debug))` block runs and formats. Each concurrent failed-or-not root can hold ≈ 100 × 500 chars. Serilog hosts must lower `MinimumLevel` to Debug, which floods every sink. When disabled, the default Debug rule still makes MEL consult the provider on every Debug call in every category. | Step 06 `ChronicleFlightRecorderLogger.IsEnabled`, `ChronicleFlightRecorderFilterSetup`; brief release-notes bullet on Serilog `writeToProviders` | public consumers that opt in; all for the disabled-path check | M | M | opt-in (`FlightRecorder.Enabled = false`); `MaxEntries` cap | CRM-8 → 06, 15 |
| 9 | **Long-lived roots grow overflow groups without bound.** Beyond `MaxSteps`, each node keeps a `Dictionary<string, ChronicleOverflow>` keyed by group key. A root that never closes (a `BackgroundService` that wraps its loop in `Begin`, a multi-hour job, or the zombie root from #2) gains one entry per distinct SQL statement hash or interpolated step name, forever. | Step 02 `ChronicleNode` overflow aggregates; step 07 group key `name#hash` | public consumers | M | L | none — gap | CRM-9 → 02, 15 |
| 10 | **Span explosion.** Step 05 starts an `Activity` for "every step", and overflow or depth-dropped handles are still `ChronicleStep`s. A 10,000-iteration loop emits 10,000 spans despite `MaxSteps`. SQL leaves would duplicate SqlClient/EF spans, and interpolated step names create high-cardinality span names that also defeat N+1 aggregation. | Step 05 `ChronicleStep` `Activity?`; step 02 overflow handle; `DreamTravel.Api/Program.cs:45-46` subscribes `OperationsName` | sample apps + public OTel consumers | M | M | `EmitActivities = false` opt-out; `HasListeners()` gate | CRM-10 → 05, 07, 15 |
| 11 | **PII in SQL error messages.** Step 07 maps `WriteCommandError` to `Fail(exceptionType, message)` regardless of `CaptureSQLCommandText`. SqlClient messages echo literal values ("The duplicate key value is (…)", "Conversion failed when converting the nvarchar value '…'"). Truncation is not masking. `Set(key, value)` also records raw values by design. | Step 07 "`Error` → `Fail(exceptionType, message)`"; step 01 `Set` | all | M | M | `CaptureSQLCommandText = false` covers the statement text only — gap for messages | CRM-11 → 07, 15 |
| 12 | **Binary break in consumers' own libraries.** An internal company package compiled against Logging 1.x injects `ITimingService`. The app upgrades to Logging 2.0, builds green, and fails with `TypeLoadException` or a DI error when that type is first resolved. `SOLCORE001` only compares `SolTechnology.Core.*` package majors, so it stays silent. | CH-08 deletes `ITimingService.cs:15-33`; flagship RM-1 (`2026-11-02-core-2-0-flagship-hardening/steps/04-bump-version-2-0-0.md:30`) | public consumers | H | L | user decision U3/CH-08 (accepted break in a major); `ValidateOnBuild` surfaces it at startup | CRM-12 → 15 (documentation); accepted risk |
| 13 | **Instrumentation exceptions escape into business code.** Step 01 lock-guards a node's children, but its attributes are a "lazy array" with no stated guard. `Current.Set` from `Task.WhenAll` branches, or from HTTP retries (B2 red 3), writes one node concurrently. The result is lost keys or an `IndexOutOfRangeException` thrown from `Set` into the handler: a 500, an abandoned message, or a Hangfire retry. | Step 01 `ChronicleNode` ("attributes (lazy array), children (lock-guarded list)"); B2 red team 3 | all | M | M | `ParallelAppends_ToOneParent_LoseNoChildren` (children only); `Dispose` never throws | CRM-13 → 01 |
| 14 | **Hot-path cost of the default-on report.** `Enabled = true`, plus CP-03, means every 2.0 request, command, and job builds a tree and a report. Step 04 builds the report before it knows whether the category level or sampling will drop it. Allocation tests cover only the disabled and no-root paths. | Step 04 `ChronicleReporter`; step 01 allocation tests | all | M | M | lazy cached `ToString` (step 03); disabled-path zero-alloc tests | CRM-14 → 04 |
| 15 | **Other SqlClient subscribers bypass the gate.** `DiagnosticListener.IsEnabled(name)` is true when **any** subscriber's predicate is true, and `Write` calls every observer's `OnNext`. With OTel SqlClient instrumentation or the App Insights SDK present, Chronicle receives `Before` outside roots and `After` without a pending entry. Step 07 relies on its own predicate to gate. | Step 07 "`IsEnabled` predicate" | public consumers with SQL instrumentation | M | M | none — gap | CRM-15 → 07 |
| 16 | **Partial upgrade across packages.** Logging 2.0 next to a 1.x integrator: the 1.x package makes no Chronicle calls, so it produces no steps but does not crash (the CQRS 1.x `Operations` break is owned by flagship RM-1). A 2.0 integrator next to Logging 1.x cannot restore, because the nuspec minimum is 2.0.0 (NU1605). CP-01 adds the Tale → Logging edge at the same version. | Flagship RM-1 `SOLCORE001`; step 11 pack check (`nuspec` dependency line) | public consumers | M | L | `SOLCORE001` + nuspec minimum versions + step 11 pack check | none new — defended |
| 17 | **`AsyncLocal` leakage across requests, parallel branches, and `Task.Run`.** Ambient writes inside the async `LoggingMiddleware.InvokeAsync` are undone when it returns. `InMemoryEventPublisher`'s `Task.Run` flows the request context into the event dispatch. `BeginRoot` separates the event into its own root, and Dispose restores the ambient node only when it is still current. | `LoggingMiddleware.cs:31`; `src/SolTechnology.Core.CQRS/Internal/InMemoryEventPublisher.cs:25-38`; `MessageBus/Broker/MessageBusBroker.cs:176-182` (`MaxConcurrentCalls`) | all | M | L | step 01 `WhenAll_ParallelBranches_BuildsIsolatedSubtrees`, `Dispose_OutOfOrder_KeepsAmbientNodeConsistent`; step 08 `BeginRoot_InTaskRunOutlivingCaller_ReportsOwnRootAndNoLateStepInCaller`; step 10 `Publish_InsideOpenRoot_…`; step 13 `HandleMessage_TwoConcurrentMessages_TwoIndependentRoots` | none new — defended |
| 18 | **Logging loop between the flight recorder and emission.** 2154 is logged under the excluded Chronicle category. 2155 is logged under the `LoggingMiddleware` category, but only after the root has closed and at ≥ Information. Reporter failures are logged at Warning. The provider depends only on options and the ambient accessor (DI-cycle guard). | Step 06 `ChronicleFlightRecorderLogger`; step 04 reporter; step 09 `RequestEnvelopeLogger` | all | L | L | step 06 `ChronicleCategory_NeverBuffered`, `InformationEntries_NotBuffered`, `AddSolChronicle_WithFlightRecorder_ResolvesLoggerFactoryWithoutCycle`; step 01 `Dispose_ListenerThrows_DoesNotThrow` | none new — defended |
| 19 | **Log volume and provider interplay.** A CQRS-only worker writes 2137 + 2138 + 2154 for each command. A Serilog `UseSerilog()` host without `writeToProviders` silently gets no flight recorder, and `{@Chronicle}` is destructured, capped at depth 10 (below `MaxDepth` 12). | Brief "Out of scope" (2137–2139 kept); B4; step 15 Serilog bullets | public consumers | L | M | step 15 documents category silencing, Serilog wiring, and the depth cap | accepted |

#### Top 3 Risks

1. #13 — an instrumentation race throws into business code. A diagnostics feature that is on by
   default must never be able to fail a request, a message, or a job.
2. #1 — process-wide SqlClient subscription × static ambient tree duplicates SQL steps in every
   multi-host process, starting with DreamTravel's own component tests
   (`ComponentTestsFixture.cs:44-45`), and keeps undisposed test hosts alive.
3. #3 — with no total size budget, one report can reach ≈ 400 K chars, exported twice by OTel.
   It is cut by sink limits exactly where the failure is, and it drives ingestion cost.
   (Close fourth: #6, the configuration kill switch silently ignored by call order.)

#### Required Mitigations Before Merge

- **CRM-1** → [07](07-sqlclient-diagnostic-steps.md): every `ChronicleRoot` carries its owning
  recorder (internal `Owner`). The SQL observer records only into roots owned by its own host's
  recorder. `StopAsync`/`Dispose` unsubscribes from both `AllListeners` and the attached listener.
  Tests: `TwoSubscriptions_OneCommand_RecordedOnce` (two `ServiceProvider`s, both started) and
  `Subscription_Stopped_ObserverReceivesNoEvents`.
- **CRM-2** → [14](14-hangfire-job-roots.md): `OnPerformed` disposes the job root handle stored
  in `filterContext.Items` inside a `finally`, so the root closes and the ambient node is restored
  on every `OnPerformed` path, including the exception path and an `OnPerformed` that sees a
  failure from a later filter. Tests run through a real `BackgroundJobPerformer`:
  `OnPerforming_LaterFilterThrows_RootEmittedFailedAndAmbientRestored` and
  `SequentialJobsOnSameThread_SecondReportHasNoStepsOfFirst` (two jobs performed on one thread;
  after each, `IChronicle.IsActive` is `false`). Residual: if Hangfire never calls `OnPerformed`,
  the zombie root stays bounded by `MaxSteps`, `FlightRecorder.MaxEntries`, and CRM-9.
- **CRM-3** → [02](02-bounded-recording-and-options-validation.md),
  [03](03-report-model-renderer-and-n-plus-one.md), [15](15-documentation.md):
  - (02) Step names are cut to `MaxAttributeValueLength` with `[truncated]`. Test
    `StepName_TooLong_TruncatedWithMarker`.
  - (03) The renderer enforces an internal total budget of 32,768 chars. Lines beyond it are
    replaced by `+{n} lines truncated`; the root line and the marker are always present. Test
    `Render_WorstCaseDefaults_AtMost32768Chars` (256 steps × 8 attributes × 200 chars, 1,000-char
    names).
  - (15) `docs/Log.md` states the budget and that OTel `IncludeFormattedMessage` exports the tree
    as both the body and the `Chronicle` attribute.
- **CRM-4** → [03](03-report-model-renderer-and-n-plus-one.md),
  [04](04-emission-level-sampling-and-registration.md), [15](15-documentation.md):
  - (03) The builder sets an internal `HasFailedSteps`. It counts failed nodes, aggregated
    `FailedCount`, and failed overflow steps.
  - (04) The policy always emits when `HasFailedSteps`. Tests
    `SampleRate0_OkRootWithFailedChild_Emitted` and
    `SampleRate0_OkRootWithFailedOverflowStep_Emitted`.
  - (15) `docs/Log.md` warns that log-count rate and error-ratio queries are invalid with
    `SampleRate < 1`, and points to ASP.NET Core request metrics.
- **CRM-5** → [09](09-http-request-roots-and-timing-service-removal.md): a final status ≥ 500 with
  no exception calls `Fail($"HTTP{statusCode}")` on the request root, the same mapping as step 12.
  4xx stays `Ok`. Test
  `InvokeAsync_HandledExceptionMappedTo500_LogsErrorWithChronicleStatusFailed`: a
  terminal middleware sets 500 without throwing.
- **CRM-6** → [04](04-emission-level-sampling-and-registration.md),
  [16](16-dreamtravel-showcase.md):
  - (04) Only service registrations are `TryAdd`-guarded. The options `Bind`/`Configure` from the
    `IConfiguration` and `Action` overloads is applied on every call. Tests
    `AddSolChronicle_ParameterlessThenConfiguration_BindsSection`,
    `AddSolChronicle_ConfigurationThenParameterless_KeepsSection`, and
    `AddSolChronicle_ActionAfterInstallerCall_Applies`.
  - (16) The component test asserts that `IOptions<ChronicleOptions>` resolved from
    `ApiFixture` reflects the `appsettings.json` `SlowThreshold`.
- **CRM-7** → [15](15-documentation.md): `docs/Log.md` gets a "Log contract changes in 2.0" table.
  It maps each 1.x entry ("Finished request" with and without `{@Timings}`, "failed after",
  "aborted by client": template, level, EventId) to its 2.0 replacement, with a before/after KQL
  pair, the slow-request Warning, and the `SlowThreshold` knob.
- **CRM-8** → [06](06-flight-recorder.md), [15](15-documentation.md):
  - (06) `Log` checks capacity before it invokes `formatter`, and the buffer is allocated on the
    first record. With `FlightRecorder.Enabled = false` at startup, and no user rule for the alias
    or provider, the filter setup adds a `LogLevel.None` rule for the alias, so MEL never consults
    the provider. Tests `MaxEntries_Reached_FormatterNotInvoked` and
    `FlightRecorderDisabled_ProviderNotConsultedForDebug`.
  - (15) `docs/Log.md` gives the worst-case memory (`MaxEntries × MaxMessageLength × 2 bytes ×
    concurrent roots`), notes that `IsEnabled(Debug)`-guarded code runs inside roots, and shows a
    Serilog example with per-sink `restrictedToMinimumLevel: Information`.
- **CRM-9** → [02](02-bounded-recording-and-options-validation.md), [15](15-documentation.md):
  - (02) Overflow groups per node are capped (internal constant 64). Further groups fold into one
    `(other)` aggregate. Test
    `MaxSteps_Exceeded_TenThousandDistinctNames_OverflowGroupsBounded`.
  - (15) `docs/Log.md` says a root must not span a service lifetime (open one root per loop
    iteration).
- **CRM-10** → [05](05-opentelemetry-activities.md), [07](07-sqlclient-diagnostic-steps.md),
  [15](15-documentation.md):
  - (05) Inside an active root, only recorded nodes start Activities; overflow and depth-dropped
    handles start none. Test `MaxSteps_Exceeded_StartsNoActivityForDroppedSteps`.
  - (07) SQL leaves start no Activity. Test `SQLStep_WithOperationsListener_StartsNoActivity`.
  - (15) `docs/Log.md` says step names must be constant (no interpolated IDs; put them in
    attributes).
- **CRM-11** → [07](07-sqlclient-diagnostic-steps.md), [15](15-documentation.md):
  - (07) With `CaptureSQLCommandText = false`, a SQL error step records the exception type only;
    the message is recorded only when capture is on, cut to `MaxAttributeValueLength`. Test
    `Error_CaptureCommandTextOff_MessageNotRecorded`.
  - (15) `docs/SQL.md` states this. It also states that the SQL step duration is what SqlClient
    measures between `Before` and `After`; the step 07 integration test confirms whether reader
    row streaming is included. `docs/Log.md` states that `Set` records raw values and that
    `SetMasked` is required for PII.
- **CRM-12** → [15](15-documentation.md): the `Migrating from ITimingService (1.x)` subsection
  states that the removal is binary-breaking. Every assembly compiled against Logging 1.x that
  references `ITimingService` must be rebuilt; `SOLCORE001` does not detect it, and
  `ValidateOnBuild` surfaces it at startup.
- **CRM-13** → [01](01-chronicle-core-step-tree.md): attribute, status, and failure writes on a
  `ChronicleNode` happen under the node lock, the same lock as children. Test
  `ParallelSet_OnSharedNode_NoExceptionAndNoLostKeys`: 64 `Task.WhenAll` branches call
  `Current.Set` with distinct keys and `MaxAttributesPerStep = 64`, and every key is present.
- **CRM-14** → [04](04-emission-level-sampling-and-registration.md): the reporter derives the level
  from status, duration, and HTTP status **before** it builds the report, and skips building when
  `!logger.IsEnabled(level)`. Rendering runs only when a provider formats the entry. Tests
  `CategoryAboveLevel_OkRoot_ReportNotBuilt` (internal builder seam counted) and
  `Root_TenSteps_Emitted_AllocatesUnderCeiling`; the ceiling is pinned at the first green
  measurement + 25% and recorded under `Deviations`.
- **CRM-15** → [07](07-sqlclient-diagnostic-steps.md): `OnNext` re-checks the gate. `Before`
  records only inside an open root owned by this recorder (CRM-1). `After`/`Error` without a
  matching pending entry are ignored, and the pending counter is decremented on every completion
  path. Test `OtherSubscriberEnabled_BeforeOutsideRoot_RecordsNothing` uses a second always-true
  subscriber.

#### Accepted Risks

- #12 — deleting `ITimingService` is a user decision (U3, CH-08) for a major version; CRM-12
  documents the binary break. No shim, because the user chose to delete the interface.
- #16 — the partial-upgrade residual stays with flagship RM-1: consumers who suppress
  `SOLCORE001`, or restore from a stale lock file, rely on the release note.
- #19 — 2137–2139 stay by scope decision (brief "Out of scope"). The Serilog behaviour is B4/B6 as
  designed and documented in step 15.
- #7 residual — the log-contract change itself was accepted by the user (OQ-2); CRM-7 only
  reduces the migration cost.
- #2 residual — if Hangfire never calls `OnPerformed` for the Chronicle filter, a zombie root stays
  on that worker thread. Its size is bounded (`MaxSteps`, `FlightRecorder.MaxEntries`, CRM-9) and
  it is never emitted.
- Step 14 reads `RetryCount` through `GetJobParameter` once per perform, one storage read
  comparable to the existing correlation read at `CorrelationIdJobFilter.cs:32`.
- Tail sampling is per root and not trace-aligned (brief "Out of scope"), so a request and its
  fire-and-forget event root can be sampled differently.
- In OTel and App Insights, `Chronicle` stays tree text, not per-step fields (B6 (b) follow-up).

#### Decision

**Go with mitigations** — no `H` scenario lacks a plausible mitigation (#12 is a user-accepted,
documented major break). Every `M` scenario has a named control or a CRM folded into its step. The
`MAJOR` classification matches the diff, and no new user decision is needed: CRM-1…15 change
internal behaviour, tests, and documentation only, with no public symbol change (`CLAUDE.md §2`)
and no change to the LC-01…05 templates.

## Deviations

### Premortem ran before the cross-feature dependencies of steps 01–04 were done (2026-11-02)

- **Planned:** run step 00 after the cross-feature dependencies of steps 01–04 are `done`, so the
  evidence is fresh.
- **Actual:** the orchestrating session ran it on 2026-11-02 at `master@ae2e1ae3`, while the
  `2026-11-02-core-2-0-logging` and `-cqrs` steps were still `to-do`. Evidence is current `master`
  plus the plan text of those features.
- **Impact:** the cited seams (`LoggingMiddleware` entries, `InMemoryEventPublisher` `Task.Run`,
  `CorrelationIdJobFilter` capture/restore, `MessageBusReceiver` visibility, the HTTP handler
  chain) may move. Each CRM is phrased against behaviour, not line numbers. If a dependency step
  removes a CRM's trigger, the executing step records that under its own `Deviations` instead of
  dropping the CRM silently.
