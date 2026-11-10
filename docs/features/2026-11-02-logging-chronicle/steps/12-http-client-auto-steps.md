---
spec: 2026-11-02-logging-chronicle
step: 12
status: done
---

# Step 12: HTTP client auto-steps — one logical step per call, attempts counted

## Summary
Every call through a client registered with `AddSolHTTPClient` becomes one Chronicle step. An
outermost internal handler records the logical call: method, client name, host, final status code,
and the number of attempts. An innermost handler, placed after the resilience handler, counts the
attempts, so retries show as `Attempts=3` on one step instead of three sibling steps. The path and
query are never recorded (PII). Calls in a loop aggregate by client and method, so the N+1 hint
also catches chatty HTTP. This is a logical span around HttpClient's own physical spans (B7).

## Affected components
- `src/SolTechnology.Core.HTTP/Handlers/ChronicleHTTPHandler.cs` — NEW — `internal sealed class ChronicleHTTPHandler(IChronicle chronicle, string clientName) : DelegatingHandler`
- `src/SolTechnology.Core.HTTP/Handlers/ChronicleHTTPAttemptHandler.cs` — NEW — `internal sealed class ChronicleHTTPAttemptHandler : DelegatingHandler`; increments the attempt counter for every attempt
- `src/SolTechnology.Core.HTTP/ModuleInstaller.cs` — EDIT — the private `AddSolHTTPClientInternal`, shared by both `AddSolHTTPClient` overloads, calls `services.AddSolChronicle()` (CP-03), adds the outer handler right after `AddHttpClient`, before the optional `CorrelationPropagatingHandler` (`propagateCorrelation`, default `true`), and adds the attempt handler right after `AddResilienceHandler($"core-http-{httpClientName}", …)`. The file is 142 lines at `c30d8998`, so the registration code lives in a helper and the installer stays ≤ 150 lines.
- `src/SolTechnology.Core.HTTP/Handlers/ChronicleHTTPHandlerRegistration.cs` — NEW — `internal static`: `AddChronicleCallHandler(IHttpClientBuilder, string httpClientName)` (`AddHttpMessageHandler(sp => new ChronicleHTTPHandler(sp.GetRequiredService<IChronicle>(), httpClientName))`) and `AddChronicleAttemptHandler(IHttpClientBuilder)` (`TryAddTransient` + `AddHttpMessageHandler<ChronicleHTTPAttemptHandler>()`)
- `tests/SolTechnology.Core.HTTP.Tests/SolTechnology.Core.HTTP.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (version from `Directory.Packages.props`, added by step 04)
- `tests/SolTechnology.Core.HTTP.Tests/ChronicleHTTPStepTests.cs` — NEW — `RichardSzalay.MockHttp` as the primary handler (already referenced)
- `tests/SolTechnology.Core.HTTP.Tests/ModuleInstallerTests.cs` — EDIT

## Changes
- Step name `HTTP {Method} {clientName}`; attributes `Host`, `StatusCode`, `Attempts`.
- Attempt counting: the outer handler stores a `StrongBox<int>` in `HttpRequestMessage.Options` under an internal `HttpRequestOptionsKey<StrongBox<int>>`. The attempt handler increments it. The box is a reference, so request snapshots made by the resilience pipeline share it.
- Outcome mapping:
  - 2xx–4xx response → `Ok` (4xx is often expected; the status is visible as `StatusCode`).
  - 5xx final response → `Fail($"HTTP{statusCode}")`.
  - Exception inside the handler chain (`HttpRequestException`, Polly `TimeoutRejectedException` from the per-attempt or total timeout, `BrokenCircuitException`) → `Fail(exception)`.
  - `HttpRequestFailedException` is not seen here: `RequestBuilder` throws it after the response has
    passed the handler chain (`RequestBuilder.BodyCapture.cs`), so the call's step is already
    closed as `Failed` (`HTTP{statusCode}`) or `Ok` (4xx).
  - Caller cancellation (`OperationCanceledException` with the caller token cancelled) → `Cancel()`.
- Outside a root, `chronicle.Step` returns a no-op handle; the handlers add no allocation beyond the `StrongBox` (allocated only when the step is recording).
- Attempts follow the 2.0 retry rules: POST and PATCH are not retried after an exception or timeout
  (P-10), zero retries leaves the retry strategy out, and `UsePolly = false` runs no strategy, so
  those calls report `Attempts=1`.
- Clients registered with `propagateCorrelation: false` still get both Chronicle handlers.
- Tests (`FakeLogCollector`; tree assertions on `FakeLogRecord.Message`):
  - `Send_InsideRoot_RecordsOneStepWithMethodClientHostAndStatus`.
  - `Send_TransientFailureThenSuccess_OneStepWithAttempts2`.
  - `Send_500AfterRetries_StepFailedWithAttemptsCount`.
  - `Send_404_StepOkWithStatusCode404`.
  - `Send_CallerCancelled_StepCancelled`.
  - `Send_Timeout_StepFailed`.
  - `Send_PostAfterTimeout_StepFailedWithAttempts1` (P-10).
  - `Send_PropagateCorrelationFalse_StillRecordsStep`.
  - `Send_PathAndQuery_NeverRecorded`.
  - `Send_OutsideRoot_RecordsNothing`.
  - `Send_FortyTwoCallsSameClient_AggregatedWithNPlusOneHint`.
  - `AddSolHTTPClient_RegistersChronicleAndHandlersInOrder`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green.
- [ ] `grep -rn "RequestUri" src/SolTechnology.Core.HTTP/Handlers/ChronicleHTTPHandler.cs | grep -v "Host"` prints nothing.
- [ ] `wc -l < src/SolTechnology.Core.HTTP/ModuleInstaller.cs` prints ≤ 150.
- [ ] `dotnet build SolTechnology.Core.slnx` is green, and `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.

## Open questions
- none

## Deviations

### Scripted primary handler and test layout

**Original plan:** `ChronicleHTTPStepTests.cs` with `RichardSzalay.MockHttp` as the primary
handler; `AddSolHTTPClient_RegistersChronicleAndHandlersInOrder` in `ModuleInstallerTests.cs`.

**Actual implementation:** the timeout and caller-cancellation cases need a response that observes
each attempt's token, which MockHttp 7.0 `Respond` overloads do not pass, so the tests use a small
`ScriptedHandler` (attempt `n` answers with script entry `n`) in `ChronicleHTTPTestBase.cs`. The
tests are split into `ChronicleHTTPStepTests` (outcomes) and `ChronicleHTTPScopeTests` (scope,
PII, aggregation, registration). `ModuleInstallerTests.cs` is already 356 lines, so the order test
lives in `ChronicleHTTPScopeTests`; it walks the handler chain from `IHttpMessageHandlerFactory`
and expects `ChronicleHTTPHandler`, `CorrelationPropagatingHandler`, `ResilienceHandler`,
`ChronicleHTTPAttemptHandler` in that order. `Send_Timeout_StepFailed` is parameterized on GET
(three attempts) and POST (one attempt, P-10), which replaces `Send_PostAfterTimeout_StepFailedWithAttempts1`.

### Smaller choices

- Outside a root the handler skips the attributes and the `StrongBox` entirely; with an
  `OperationsName` listener the call still gets an activity-only span without tags.
- A cancelled or failed call records `Attempts` but no `StatusCode` (there is no response).

