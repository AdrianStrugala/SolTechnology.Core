---
spec: 2026-11-02-logging-chronicle
step: 16
status: done
---

# Step 16: DreamTravel showcase — Chronicle configuration, README report, component test

## Summary
DreamTravel is the reference app, so it shows Chronicle working end to end. The API and Worker
bind `Logging:Core:Chronicle` from configuration, Development enables the flight recorder and
multi-line console output, and the README shows a real `FindCityByName` report. A component test
proves that one request produces exactly one EventId 2155 entry whose tree contains the CQRS query
step with nested HTTP steps. The call-site migration itself happened in step 09.

## Affected components
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Program.cs` — EDIT — `builder.Services.AddSolChronicle(builder.Configuration);` next to `AddSolLogging()`
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/appsettings.json` — EDIT — `Logging:Core:Chronicle` section (`SlowThreshold`, `SampleRate`, `CaptureSQLCommands`, `CaptureSQLCommandText: false`); `Logging:LogLevel` entry `SolTechnology.Core.Logging.Chronicle: Information`
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/appsettings.Development.json` — EDIT — `Logging:Core:Chronicle:FlightRecorder:Enabled: true`; `Logging:Console:FormatterOptions:SingleLine: false`
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Worker/Program.cs` — EDIT — `builder.Services.AddSolChronicle(builder.Configuration);` next to `AddSolCQRS`/`AddSolPersistentEvents`/`AddSolRecurringJob` (the Worker does not call `AddSolLogging`)
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Worker/appsettings.json` — EDIT — `Logging:Core:Chronicle` section
- `sample-tale-code-apps/DreamTravel/README.md` — EDIT — "Chronicle" section with the report produced by the component test run
- `sample-tale-code-apps/DreamTravel/tests/Component/ComponentTestsFixture.cs` — EDIT — `ApiFixture` (`APIFixture<Program>`, whose constructor takes `IConfiguration? configuration` and `Action<IServiceCollection>? configureServices`) built with `configureServices: services => services.AddLogging(l => l.AddFakeLogging())`
- `sample-tale-code-apps/DreamTravel/tests/Component/DreamTravel.Component.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (no version: DreamTravel resolves it from the root `Directory.Packages.props` entry added by step 04)
- `sample-tale-code-apps/DreamTravel/tests/Component/Trips/FindCityChronicleTests.cs` — NEW

## Changes
- Test `FindCityByName_OneRequest_EmitsOne2155EntryWithNestedCQRSAndHTTPSteps`:
  - Arrange the Google fake API through `WireMockFixture`, as `FindCityAndSaveDetailsTest` does, for a city name absent from the seeded database (`CityDomainService.Get` reads EF Core first and calls Google only on a miss). WireMock matching is strict since the testing kits' P-11, so the stub must match the exact request the Google client sends.
  - Act: one API call that runs `FindCityByNameQuery`.
  - Assert (on `FakeLogRecord.Message`): exactly one 2155 record for the request path; its report contains `FindCityByNameQuery` with child `FetchCity`, and `FetchCity` contains at least one `SQL SELECT` step (EF Core over SqlClient, step 07) and at least one `HTTP GET Google` step; the status is `Ok`; the report has no `Event CitySearched` step (the event is persisted to Hangfire and handled in the Worker).
- Test `FindCityByName_UnknownCity_ReportShowsFailedStep` with an explicit error stub for that city (an unmatched request would get WireMock's own 404, which is not the case under test): one 2155 entry, level ≥ Warning.
- Read the report from `FakeLogCollector` through `ApiFixture.TestServer.Services.GetFakeLogCollector()`.
- No secrets are added; configuration keys hold non-secret values only.
- Premortem mitigations (step 00, 2026-11-02):
  - CRM-6: the component test asserts that `IOptions<ChronicleOptions>` resolved from
    `ApiFixture` reflects the `SlowThreshold` from `DreamTravel.Api/appsettings.json`, although
    `AddSolCQRS()` also calls the parameterless `AddSolChronicle()`. Test
    `ChronicleOptions_BoundFromAppsettings_DespiteInstallerDefaults`.

## Acceptance criteria
- [ ] `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [ ] `cd sample-tale-code-apps/DreamTravel && dotnet test tests/Component --filter "FullyQualifiedName~FindCityChronicle"` is green (Docker available).
- [ ] `cd sample-tale-code-apps/DreamTravel && dotnet test tests/Component` is green (no regression from the fixture change).
- [ ] `grep -c "Chronicle" sample-tale-code-apps/DreamTravel/README.md` prints at least `1`, and the README tree came from a real run.
- [ ] `git diff -- sample-tale-code-apps/DreamTravel | grep -iE "password|secret|apikey" | grep "^+"` prints nothing.
- [ ] CRM-6: `ChronicleOptions_BoundFromAppsettings_DespiteInstallerDefaults` is green.

## Open questions
- none

## Deviations

### Finding the report and the failure stub

**Original plan:** assert "exactly one 2155 record for the request path"; the failure test uses
"an explicit error stub for that city".

**Actual implementation:** the fake log collector is shared by every API test, and other tests call
the same path, so each test uses a unique city name and selects the 2155 record by its query
attribute `Name={city}` (the `[LogScope]` property of `FindCityByNameQuery`). A correlation header
was not used: inbound trace context would replace it. The failure stub answers 404, not 5xx, so the
test cannot open the shared Google circuit breaker for later tests; the Google client turns the 404
into an `InvalidDataException`, the query step and the root are `Failed`, and the response is 500.
The API key header is read from the test configuration, so no secret placeholder is added.

### What the real report shows

The README tree is the run's output (`TestContext.Out`). Besides the planned steps it shows SQL
commands at the root (before the query step) and the `SQL SET`/`SQL SELECT`/`SQL INSERT` of the
Hangfire SQL Server storage enqueuing `CitySearched`, which confirms the event is persisted, not
dispatched in the request. The Worker binds a `SlowThreshold` of 5 seconds, because its roots are
jobs.

