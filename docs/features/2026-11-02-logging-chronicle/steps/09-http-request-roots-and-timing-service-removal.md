---
spec: 2026-11-02-logging-chronicle
step: 09
status: to-do
---

# Step 09: HTTP request roots in `LoggingMiddleware`; remove `ITimingService`

## Summary
Every non-skipped HTTP request becomes a Chronicle root, and its report **is** the request finish
entry (B5): one EventId 2151 entry per request, with the method, path, status code, and elapsed
time kept as scalar placeholders. A failed request logs one Error 2151 entry with the exception
(P-15). A client abort logs a Warning with status `Cancelled`. `ITimingService` and
`TimingService` are deleted, and the only external call site (DreamTravel
`FindCityByNameHandler`) moves to `IChronicle` in the same PR, so every build stays green. With
`Enabled = false` the middleware keeps the finish entries it logs today, without `{@Timings}`.

## Affected components
- `src/SolTechnology.Core.Logging/Middleware/LoggingMiddleware.cs` — EDIT — `InvokeAsync(HttpContext, ILogger<LoggingMiddleware>, ICorrelationIdService, IEnumerable<ILogScopeEnricher>, IChronicle chronicle)` (CH-09); remove `timingService.Reset()`; begin the request root after the skip-path check; hand the closed root to `RequestEnvelopeLogger`
- `src/SolTechnology.Core.Logging/Middleware/RequestEnvelopeLogger.cs` — EDIT (created by `2026-11-02-core-2-0-logging` step "Guide conformance — Logging") — when the root recorded: log 2151 only; otherwise the existing finish/failed/aborted entries without `{@Timings}`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRequestRoot.cs` — NEW — internal static: `Begin(IChronicle chronicle, HttpContext context)` and `Complete(ChronicleStep root, HttpContext context, Exception? exception, bool clientAborted)`; sets attributes `Route` (`RouteEndpoint.RoutePattern.RawText`, never the raw path) and `StatusCode`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRecorder.cs` — EDIT — `BeginWithoutActivity(string name, bool reportedByCaller)` sets `ChronicleRoot.IsReportedByCaller` (step 04 reporter skips such roots)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleStep.cs` — EDIT — `internal ChronicleRoot? Root` accessor for the middleware
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleLogMessages.cs` — EDIT — `[LoggerMessage(EventId = 2151, EventName = "RequestChronicleReported", Message = "Finished request [{RequestMethod}] [{RequestPath}] -> [{StatusCode}] in [{ElapsedMs} ms]. Chronicle: [{ChronicleStatus}] [{@Chronicle}]")] static partial void RequestChronicleReported(ILogger logger, LogLevel level, Exception? exception, string requestMethod, string requestPath, int statusCode, long elapsedMs, string chronicleStatus, ChronicleReport chronicle)` (LC-01)
- `src/SolTechnology.Core.Logging/ITimingService.cs` — DELETE (CH-08)
- `src/SolTechnology.Core.Logging/TimingService.cs` — DELETE
- `src/SolTechnology.Core.Logging/LoggingServiceRegistrations.cs` — EDIT (created by `2026-11-02-core-2-0-logging` step "Guide conformance — Logging") — remove the `ITimingService` → `TimingService` registration
- `src/SolTechnology.Core.Logging/ModuleInstaller.cs` — EDIT — remove every `ITimingService` mention from XML docs (and the registration, if it still lives here)
- `tests/SolTechnology.Core.Logging.Tests/TimingServiceTests.cs` — DELETE
- `tests/SolTechnology.Core.Logging.Tests/Middleware/LoggingMiddlewareTests.cs` — EDIT — replace `ITimingService` substitutes with the real recorder from `AddSolChronicle()` and a `FakeLogCollector`
- `tests/SolTechnology.Core.Logging.Tests/Middleware/LoggingMiddlewareChronicleTests.cs` — NEW
- `tests/SolTechnology.Core.Logging.Tests/ModuleResolutionTests.cs` — EDIT (created by `2026-11-02-core-2-0-logging` step "DI resolution test — Logging") — drop the `ITimingService` resolution; keep `IChronicle`
- `tests/SolTechnology.Core.Logging.Tests/ModuleInstallerTests.cs` — EDIT — drop `ITimingService` assertions, if any
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/FindCityByName/FindCityByNameHandler.cs` — EDIT — inject `IChronicle chronicle`; `using (chronicle.Step("FetchCity"))` and `using (chronicle.Step("FetchCityCached"))` replace the two `StartContext` blocks
- `docs/Log.md` — EDIT — remove the `ITimingService` section, its "Features" bullet, and its "What ships in DI" bullet; step 15 writes the Chronicle section and the migration subsection

## Changes
- Root name: `HTTP {Method}`. The route template is added at completion as `Route`; requests with no endpoint get `Route=unmatched`.
- The root starts through `BeginWithoutActivity(name, reportedByCaller: true)`: ASP.NET Core already owns the request Activity, and the reporter (step 04) must not emit a second entry.
- Fallback when `IChronicle` is not `ChronicleRecorder` (a user replacement): `chronicle.Begin(name)`, and the middleware logs the same entries as with `Enabled = false`. A request never ends without a finish entry; the replacement owns any report.
- Status and level:
  - Unhandled exception → `Fail(exception)` → Error 2151 with the exception attached (LC-02). The "Request [..] failed after [..]" entry is not logged.
  - Client abort (the existing `IsClientAbort` check) → `Cancel()` → Warning 2151 (LC-02).
  - Otherwise `ChronicleEmissionPolicy.Decide(report, statusCode)` (step 04): 5xx → Error, 4xx → Warning, slow → Warning, else Information.
- Sampling (LC-04) applies; failures and slow requests are always emitted.
- `RequestPath` keeps the value the existing finish entry uses, so existing KQL keeps working.
- Tests (`LoggingMiddlewareChronicleTests`, `FakeTimeProvider`, `FakeLogCollector`; tree assertions on `FakeLogRecord.Message`, see the brief's testing conventions):
  - `InvokeAsync_SuccessfulRequest_EmitsOneEntryWithEventId2151`.
  - `InvokeAsync_EndpointSteps_ReportContainsNestedSteps`.
  - `InvokeAsync_SlowRequest_LogsWarning`.
  - `InvokeAsync_404_LogsWarning`.
  - `InvokeAsync_HandlerThrows_LogsExactlyOneErrorEntryWithException`.
  - `InvokeAsync_ClientAbort_LogsWarningCancelled`.
  - `InvokeAsync_ChronicleDisabled_LogsPreviousFinishEntryWithoutTimings`.
  - `InvokeAsync_ReplacedChronicle_LogsPreviousFinishEntry`.
  - `InvokeAsync_SkippedPath_EmitsNoReport`.
  - `InvokeAsync_RequestRoot_StartsNoOperationsActivity`.
  - `InvokeAsync_RequestRoot_NoEventId2150Entry`.
  - `InvokeAsync_RouteAttribute_UsesTemplateNotRawPath`.
  - `RequestChronicleReported_Template_EveryPlaceholderBracketed`.
- Premortem mitigations (step 00, 2026-11-02):
  - CRM-5: when the final status code is ≥ 500 and no exception reached the middleware (for
    example `SolTechnology.Core.Api`'s `ExceptionFilter` mapped it and set `ExceptionHandled`),
    `ChronicleRequestRoot.Complete` calls `Fail($"HTTP{statusCode}")` on the root, the same
    mapping as step 12. 4xx stays `Ok`. Test
    `InvokeAsync_HandledExceptionMappedTo500_LogsErrorWithChronicleStatusFailed` (a terminal
    middleware sets 500 without throwing → one Error 2151 entry, `ChronicleStatus = Failed`, no
    exception attached).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [ ] `grep -rn "ITimingService\|TimingService" src tests sample-tale-code-apps --include=*.cs | grep -v "/bin/\|/obj/"` prints nothing.
- [ ] `grep -n "ITimingService" docs/Log.md` prints nothing.
- [ ] `grep -rnE "EventId = 2151" src --include=*.cs | wc -l` prints `1`.
- [ ] `grep -n "Timings" src/SolTechnology.Core.Logging -r --include=*.cs` prints nothing.
- [ ] `dotnet build SolTechnology.Core.slnx` is green, and `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [ ] `cd sample-tale-code-apps/DreamTravel && dotnet test tests/Unit/DreamTravel.Queries.UnitTests` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.
- [ ] CRM-5: `InvokeAsync_HandledExceptionMappedTo500_LogsErrorWithChronicleStatusFailed` is green.

## Open questions
- none (OQ-1 confirmed 2026-11-02: `CH-08` delete `ITimingService`, `CH-09` `InvokeAsync` parameter; OQ-2 accepted 2026-11-02: `LC-01`, `LC-02`)

## Deviations
