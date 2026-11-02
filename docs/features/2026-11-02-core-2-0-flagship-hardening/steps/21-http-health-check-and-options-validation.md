---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 21
status: to-do
---

# Step 21: HTTP — upstream health-check disposal and options validation

## Summary
Two lifecycle defects and one validation gap in HTTP:

- `BaseUpstreamServiceHealthCheck` never disposes the `HttpResponseMessage` from `GetAsync`, so
  each probe holds a connection until GC (`http-api#8`).
- The HTTP installer's `AddOptions<TOptions>()` chain has no `ValidateDataAnnotations()` and no
  `ValidateOnStart()`, although the docs say every option type uses `ValidateOnStart`
  (`http-api#15`, Guide §14).
- The health-check XML docs disagree with the code (`http-api#9`, doc part; `http-api#22`).

The `IHttpClientFactory` constructor change (`C-11`) is in step 45.

## Affected components
- `src/SolTechnology.Core.HTTP/HealthChecks/BaseUpstreamServiceHealthCheck.cs` — EDIT — `:80` `using var response`; `:15` XML doc (failures are cached too)
- `src/SolTechnology.Core.HTTP/HealthChecks/UpstreamHttpHealthCheckExtensions.cs` — EDIT — `:14-15` XML doc names only existing parameters
- `src/SolTechnology.Core.HTTP/ModuleInstaller.cs` — EDIT — `:50` `.ValidateDataAnnotations().ValidateOnStart()`
- `tests/SolTechnology.Core.HTTP.Tests/ModuleInstallerTests.cs` — EDIT — invalid options fail host start
- `tests/SolTechnology.Core.HTTP.Tests/BaseUpstreamServiceHealthCheckTests.cs` — EDIT — response disposed

## Changes
- Tests:
  - `AddSolHTTPClient_InvalidOptions_HostStartFails`, using a `[Range]` violation on a test options type.
  - `CheckHealth_DisposesResponse`, using a `DelegatingHandler` that tracks disposal.
- Premortem RM-13 (step 00, 2026-11-02, scenario 13): append a `P-nn` row to the brief's "Behavioural and persisted-contract breaks" table: HTTP client options are validated with data annotations at host start, so options that 1.x accepted can now stop the host with `OptionsValidationException`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green.
- [ ] DreamTravel starts with its current config: `dotnet test sample-tale-code-apps/DreamTravel/tests/Component/DreamTravel.Component.Tests.csproj` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [ ] RM-13: the brief's P-table has a row for the HTTP options validation.

## Open questions
- none

## Deviations
