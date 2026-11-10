---
spec: 2026-11-02-logging-chronicle
step: 09
status: done
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

### Outcome logging split between `RequestEnvelopeLogger.RequestEnded` and `ChronicleRequestRoot`

**Original plan:** `ChronicleRequestRoot` has `Begin` and `Complete`; `RequestEnvelopeLogger` logs
2155 when the root recorded; the emission policy is `ChronicleEmissionPolicy.Decide`.

**Actual implementation:** the middleware makes one call per outcome,
`RequestEnvelopeLogger.RequestEnded(logger, context, root, exception, clientAborted, elapsedMs)`,
which keeps `LoggingMiddleware.cs` at 150 lines. `ChronicleRequestRoot.Complete` sets `Route` and
`StatusCode`, maps the outcome, disposes the handle, and returns the closed root only when it is a
root this middleware must report (`IsReportedByCaller`); otherwise `RequestEnded` writes 2144, 2145,
or 2146. `ChronicleRequestRoot.Report` writes 2155 through the step 04 split policy (`LevelOf` with
the status code, then `ShouldEmit`); the policy instance lives on `ChronicleWriter.Policy`. A
report failure is logged as a Warning, like the reporter does. The root starts in `InvokeAsync`
right after the skip-path check, so it also covers scope building (a Debug 2148 lands in the
flight recorder). `ChronicleStep.Root` returns only a root node the handle owns, so a request
running inside an already open root (a test host, a nested pipeline) becomes a child step and the
middleware falls back to 2146.

### Smaller choices

- The client-abort 2155 entry keeps the exception attached, as 2144 did; `ChronicleStatus` is
  `Cancelled`.
- `ElapsedMs` stays the middleware stopwatch (it includes scope building), like 2146; the tree
  shows the root duration from `TimeProvider`.
- Tests: `InvokeAsync_SlowRequest_LogsWarning` and `InvokeAsync_404_LogsWarning` are one
  parameterized `InvokeAsync_SlowRequestOr404_LogsWarning`; `InvokeAsync_RequestRoot_NoEventId2154Entry`
  is folded into the success test (its single finish entry is counted over 2144–2147, 2154, and
  2155); `InvokeAsync_ReplacedChronicle_LogsPreviousFinishEntry` also covers 2145; the route test
  also covers `Route=unmatched`. The tests are split into `LoggingMiddlewareChronicleTests` and
  `LoggingMiddlewareChronicleFallbackTests` over `LoggingMiddlewareChronicleTestBase`, and
  `TestChronicles` builds the real recorder for the older middleware tests. The envelope and body
  buffering tests use a disabled recorder (the `Enabled = false` entries); `LoggingMiddlewareTests`
  uses an enabled one, so its single-Error test now asserts the 2155 `Failed` entry.
  `RequestEnvelopeLoggerTests` replaces the 2147 case with
  `RequestEnded_RecordedRootAndLevelDisabled_WritesNothing`. `ModuleInstallerTests` had no
  `ITimingService` assertion and is unchanged.

### CRM-5 re-check at implementation (2026-11-10)

Holds. `ExceptionFilter` sets `ExceptionHandled` after mapping, and `ResultConversionFilter` maps a
failed `Result` to a `ProblemDetails` status (500 for unmapped errors), so neither lets an exception
reach the middleware. `ChronicleRequestRoot.Complete` therefore fails the root with `HTTP{status}`
for any final status of 500 or more without an exception, whichever filter produced it
(`InvokeAsync_HandledExceptionMappedTo500_LogsErrorWithChronicleStatusFailed`).

