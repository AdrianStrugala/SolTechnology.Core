---
spec: 2026-11-02-logging-chronicle
step: 00
status: to-do
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
  - Log contract: `LC-01`…`LC-05` (EventId 2151 replaces the finish entry, one Error entry per failed request, new EventId 2150, sampling, flight-recorder records).
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
- [ ] Premortem output (frame, imagined failure, scenario table, top 3 risks, required mitigations naming their step files, accepted risks, decision) recorded in this file.
- [ ] `summary.md` `premortem:` updated to the verdict in the same change.

## Open questions
- none

## Deviations
