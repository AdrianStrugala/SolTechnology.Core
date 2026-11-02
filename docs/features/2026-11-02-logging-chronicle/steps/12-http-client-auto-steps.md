---
spec: 2026-11-02-logging-chronicle
step: 12
status: to-do
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
- `src/SolTechnology.Core.HTTP/ModuleInstaller.cs` — EDIT — `AddSolHTTPClient` overloads call `services.AddSolChronicle()` (CP-03); `AddHttpMessageHandler(sp => new ChronicleHTTPHandler(sp.GetRequiredService<IChronicle>(), httpClientName))` before `CorrelationPropagatingHandler`; `AddHttpMessageHandler<ChronicleHTTPAttemptHandler>()` after `AddResilienceHandler`. If `2026-11-02-core-2-0-http` step "Guide conformance — HTTP" moved the handler chain into an installer helper file, edit that file instead and list it under `Deviations`.
- `tests/SolTechnology.Core.HTTP.Tests/SolTechnology.Core.HTTP.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (same version as step 04)
- `tests/SolTechnology.Core.HTTP.Tests/ChronicleHTTPStepTests.cs` — NEW — `RichardSzalay.MockHttp` as the primary handler (already referenced)
- `tests/SolTechnology.Core.HTTP.Tests/ModuleInstallerTests.cs` — EDIT

## Changes
- Step name `HTTP {Method} {clientName}`; attributes `Host`, `StatusCode`, `Attempts`.
- Attempt counting: the outer handler stores a `StrongBox<int>` in `HttpRequestMessage.Options` under an internal `HttpRequestOptionsKey<StrongBox<int>>`. The attempt handler increments it. The box is a reference, so request snapshots made by the resilience pipeline share it.
- Outcome mapping:
  - 2xx–4xx response → `Ok` (4xx is often expected; the status is visible as `StatusCode`).
  - 5xx final response → `Fail($"HTTP{statusCode}")`.
  - Exception (including `HttpRequestFailedException`, timeouts, and broken circuits) → `Fail(exception)`.
  - Caller cancellation (`OperationCanceledException` with the caller token cancelled) → `Cancel()`.
- Outside a root, `chronicle.Step` returns a no-op handle; the handlers add no allocation beyond the `StrongBox` (allocated only when the step is recording).
- Tests:
  - `Send_InsideRoot_RecordsOneStepWithMethodClientHostAndStatus`.
  - `Send_TransientFailureThenSuccess_OneStepWithAttempts2`.
  - `Send_500AfterRetries_StepFailedWithAttemptsCount`.
  - `Send_404_StepOkWithStatusCode404`.
  - `Send_CallerCancelled_StepCancelled`.
  - `Send_Timeout_StepFailed`.
  - `Send_PathAndQuery_NeverRecorded`.
  - `Send_OutsideRoot_RecordsNothing`.
  - `Send_FortyTwoCallsSameClient_AggregatedWithNPlusOneHint`.
  - `AddSolHTTPClient_RegistersChronicleAndHandlersInOrder`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green.
- [ ] `grep -rn "RequestUri" src/SolTechnology.Core.HTTP/Handlers/ChronicleHTTPHandler.cs | grep -v "Host"` prints nothing.
- [ ] `dotnet build SolTechnology.Core.slnx` is green, and `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.

## Open questions
- none

## Deviations
