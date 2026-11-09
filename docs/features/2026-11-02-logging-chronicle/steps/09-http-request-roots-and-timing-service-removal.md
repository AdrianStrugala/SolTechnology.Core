---
spec: 2026-11-02-logging-chronicle
step: 09
status: to-do
---

# Step 09: HTTP request roots in `LoggingMiddleware`; remove `ITimingService`

## Summary
Every non-skipped HTTP request becomes a Chronicle root, and its report **is** the request finish
entry (B5): one EventId 2155 entry per request, with the method, path, status code, and elapsed
time kept as scalar placeholders. A failed request logs one Error 2155 entry with the exception
(P-15). A client abort logs a Warning with status `Cancelled`. `ITimingService` and
`TimingService` are deleted, and the only external call site (DreamTravel
`FindCityByNameHandler`) moves to `IChronicle` in the same PR, so every build stays green. With
`Enabled = false` the middleware keeps the finish entries it logs today (2144 aborted, 2145
failed, 2146 finished); 2147, the finish entry with `{@Timings}`, is deleted and its id retired.

## Affected components
- `src/SolTechnology.Core.Logging/Middleware/LoggingMiddleware.cs` — EDIT — `InvokeAsync(HttpContext, ILogger<LoggingMiddleware>, ICorrelationIdService, IEnumerable<ILogScopeEnricher>, IChronicle chronicle)` (CH-09); remove `timingService.Reset()` and the `ITimingService` parameter of the private `InvokeNextAndLogOutcome`; begin the request root after the skip-path check; hand the closed root to `RequestEnvelopeLogger`. The file is exactly 150 lines at `c30d8998`, so the root handling lives in `ChronicleRequestRoot` and the middleware stays ≤ 150 lines.
- `src/SolTechnology.Core.Logging/Middleware/RequestEnvelopeLogger.cs` — EDIT — when the root recorded: log 2155 only; otherwise the existing entries 2144 (`RequestAborted`), 2145 (`RequestFailed`), and 2146 (`RequestFinished`); `RequestFinished(ILogger, HttpContext, ITimingService, long)` loses its `ITimingService` parameter, and the 2147 `RequestFinishedWithTimings` method is deleted (the id is not reused)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRequestRoot.cs` — NEW — internal static: `Begin(IChronicle chronicle, HttpContext context)` and `Complete(ChronicleStep root, HttpContext context, Exception? exception, bool clientAborted)`; sets attributes `Route` (`RouteEndpoint.RoutePattern.RawText`, never the raw path) and `StatusCode`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRecorder.cs` — EDIT — `BeginWithoutActivity(string name, bool reportedByCaller)` sets `ChronicleRoot.IsReportedByCaller` (step 04 reporter skips such roots)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleStep.cs` — EDIT — `internal ChronicleRoot? Root` accessor for the middleware
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleLogMessages.cs` — EDIT — `[LoggerMessage(EventId = 2155, EventName = "RequestChronicleReported", Message = "Finished request [{RequestMethod}] [{RequestPath}] -> [{StatusCode}] in [{ElapsedMs} ms]. Chronicle: [{ChronicleStatus}] [{@Chronicle}]")] static partial void RequestChronicleReported(ILogger logger, LogLevel level, Exception? exception, string requestMethod, PathString requestPath, int statusCode, long elapsedMs, string chronicleStatus, ChronicleReport chronicle)` (LC-01; `PathString`, as in 2143–2146, so `RequestPath` keeps its value)
- `src/SolTechnology.Core.Logging/ITimingService.cs` — DELETE (CH-08)
- `src/SolTechnology.Core.Logging/TimingService.cs` — DELETE
- `src/SolTechnology.Core.Logging/LoggingServiceRegistrations.cs` — EDIT — remove `services.TryAddScoped<ITimingService, TimingService>()` from `RegisterCore` (both `AddSolLogging` overloads reach it)
- `src/SolTechnology.Core.Logging/ModuleInstaller.cs` — no edit expected: at `c30d8998` it neither registers nor mentions `ITimingService`; the acceptance grep confirms it
- `tests/SolTechnology.Core.Logging.Tests/TimingServiceTests.cs` — DELETE
- `tests/SolTechnology.Core.Logging.Tests/Middleware/LoggingMiddlewareTests.cs` — EDIT — replace the `NoOpTiming()` helper (`new TimingService(TimeProvider.System)`) with the real recorder from `AddSolChronicle()` and a `FakeLogCollector`
- `tests/SolTechnology.Core.Logging.Tests/Middleware/LoggingMiddlewareEnvelopeTests.cs` — EDIT — drop the `TimingService` argument; the envelope assertions keep covering the `Enabled = false` entries 2143–2146
- `tests/SolTechnology.Core.Logging.Tests/Middleware/LoggingMiddlewareBodyBufferingTests.cs` — EDIT — drop the two `new TimingService(TimeProvider.System)` arguments
- `tests/SolTechnology.Core.Logging.Tests/Middleware/RequestEnvelopeLoggerTests.cs` — EDIT — drop the `ITimingService` substitute and the 2147 case
- `tests/SolTechnology.Core.Logging.Tests/Middleware/LoggingMiddlewareChronicleTests.cs` — NEW
- `tests/SolTechnology.Core.Logging.Tests/ModuleResolutionTests.cs` — EDIT — drop the scoped `ITimingService` resolution; keep `IChronicle`
- `tests/SolTechnology.Core.Logging.Tests/ModuleInstallerTests.cs` — EDIT — drop `ITimingService` assertions, if any
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/FindCityByName/FindCityByNameHandler.cs` — EDIT — inject `IChronicle chronicle` instead of `ITimingService timingService`; `using (chronicle.Step("FetchCity"))` and `using (chronicle.Step("FetchCityCached"))` replace the `StartContext("http")` and `StartContext("cache")` blocks
- `docs/Log.md` — EDIT — remove the `ITimingService` section, its "Features" bullet, and its "What ships in DI" bullet; step 15 writes the Chronicle section and the migration subsection

## Changes
- Root name: `HTTP {Method}`. The route template is added at completion as `Route`; requests with no endpoint get `Route=unmatched`.
- The root starts through `BeginWithoutActivity(name, reportedByCaller: true)`: ASP.NET Core already owns the request Activity, and the reporter (step 04) must not emit a second entry.
- Fallback when `IChronicle` is not `ChronicleRecorder` (a user replacement): `chronicle.Begin(name)`, and the middleware logs the same entries as with `Enabled = false`. A request never ends without a finish entry; the replacement owns any report.
- Status and level:
  - Unhandled exception → `Fail(exception)` → Error 2155 with the exception attached (LC-02). The "Request [..] failed after [..]" entry (2145) is not logged.
  - Client abort (the existing `IsClientAbort` check) → `Cancel()` → Warning 2155 (LC-02). The "aborted by client" entry (2144) is not logged.
  - Otherwise `ChronicleEmissionPolicy.Decide(report, statusCode)` (step 04): 5xx → Error, 4xx → Warning, slow → Warning, else Information.
- Sampling (LC-04) applies; failures and slow requests are always emitted.
- `RequestPath` keeps the value the existing finish entry uses, so existing KQL keeps working.
- `Started request` (2143) and the scope-building entries (2141, 2142, 2148) are unchanged.
- Tests (`LoggingMiddlewareChronicleTests`, `FakeTimeProvider`, `FakeLogCollector`; tree assertions on `FakeLogRecord.Message`, see the brief's testing conventions):
  - `InvokeAsync_SuccessfulRequest_EmitsOneEntryWithEventId2155`.
  - `InvokeAsync_EndpointSteps_ReportContainsNestedSteps`.
  - `InvokeAsync_SlowRequest_LogsWarning`.
  - `InvokeAsync_404_LogsWarning`.
  - `InvokeAsync_HandlerThrows_LogsExactlyOneErrorEntryWithException`.
  - `InvokeAsync_ClientAbort_LogsWarningCancelled`.
  - `InvokeAsync_ChronicleDisabled_LogsPreviousFinishEntryWithoutTimings`.
  - `InvokeAsync_ReplacedChronicle_LogsPreviousFinishEntry`.
  - `InvokeAsync_SkippedPath_EmitsNoReport`.
  - `InvokeAsync_RequestRoot_StartsNoOperationsActivity`.
  - `InvokeAsync_RequestRoot_NoEventId2154Entry`.
  - `InvokeAsync_RouteAttribute_UsesTemplateNotRawPath`.
  - `RequestChronicleReported_Template_EveryPlaceholderBracketed`.
- Premortem mitigations (step 00, 2026-11-02):
  - CRM-5: when the final status code is ≥ 500 and no exception reached the middleware (for
    example `SolTechnology.Core.Api`'s `ExceptionFilter` mapped it and set `ExceptionHandled`, or
    its `ResultConversionFilter` turned a failed `Result` into a 5xx response),
    `ChronicleRequestRoot.Complete` calls `Fail($"HTTP{statusCode}")` on the root, the same
    mapping as step 12. 4xx stays `Ok`. Test
    `InvokeAsync_HandledExceptionMappedTo500_LogsErrorWithChronicleStatusFailed` (a terminal
    middleware sets 500 without throwing → one Error 2155 entry, `ChronicleStatus = Failed`, no
    exception attached).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [ ] `grep -rn "ITimingService\|TimingService" src tests sample-tale-code-apps --include=*.cs | grep -v "/bin/\|/obj/"` prints nothing.
- [ ] `grep -n "ITimingService" docs/Log.md` prints nothing.
- [ ] `grep -rnE "EventId = 2155" src --include=*.cs | wc -l` prints `1`.
- [ ] `grep -n "Timings" src/SolTechnology.Core.Logging -r --include=*.cs` prints nothing, and `grep -rn "EventId = 2147" src --include=*.cs` prints nothing.
- [ ] `wc -l < src/SolTechnology.Core.Logging/Middleware/LoggingMiddleware.cs` prints ≤ 150.
- [ ] `dotnet build SolTechnology.Core.slnx` is green, and `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [ ] `cd sample-tale-code-apps/DreamTravel && dotnet test tests/Unit/DreamTravel.Queries.UnitTests` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.
- [ ] CRM-5: `InvokeAsync_HandledExceptionMappedTo500_LogsErrorWithChronicleStatusFailed` is green.

## Open questions
- none (OQ-1 confirmed 2026-11-02: `CH-08` delete `ITimingService`, `CH-09` `InvokeAsync` parameter; OQ-2 accepted 2026-11-02: `LC-01`, `LC-02`)

## Deviations
