---
spec: 2026-11-02-core-2-0-tale
step: 11
status: to-do
---

# Step 11: Tale — explicit HTTP exposure; internal plumbing types

## Summary
`TaleController` exposes every scanned `TaleHandler`, including CQRS query tales and domain
services such as `CityDomainService`. Any of them can be started over HTTP with arbitrary input,
bypassing FluentValidation and the logging behaviours. `RestrictControllerToRegisteredHandlers`
is never read (`cqrs-tale#7`, `ModuleInstaller.cs:96-100`, `TaleOptions.cs:24`,
`Api/TaleController.cs:60-77`). `SchemaBuilder` adds a public extension to every `System.Type`
(`cqrs-tale#23`). This step implements `C-06` and `C-07` (P-12).

Former core step 43; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Tale/Builder/ITaleBuilder.cs` — EDIT — NEW `ITaleBuilder ExposeOverHttp<THandler>() where THandler : class`
- `src/SolTechnology.Core.Tale/Builder/` (implementation) — EDIT — records exposed handlers
- `src/SolTechnology.Core.Tale/ModuleInstaller.cs` — EDIT — `TaleHandlerRegistry.Register` → `internal`; the registry holds only exposed handlers
- `src/SolTechnology.Core.Tale/TaleOptions.cs` — EDIT — DELETE `RestrictControllerToRegisteredHandlers`
- `src/SolTechnology.Core.Tale/Models/DataField.cs` — EDIT — `SchemaBuilder` → `internal static`
- `src/SolTechnology.Core.Tale/Api/TaleController.cs` — EDIT — 404 for a non-exposed handler
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Workflows/ModuleInstaller.cs` — EDIT — `.ExposeOverHttp<…>()` for the interactive workflows the UI uses
- `tests/SolTechnology.Core.Tale.Tests/TaleControllerTests.cs` — EDIT

## Changes
- Tests:
  - `Start_NonExposedHandler_Returns404`.
  - `Start_ExposedHandler_Starts`.
  - `Start_QueryTaleNotExposed_Returns404`.
- Premortem RM-14 (core step 00, 2026-11-02, scenario 14): keep the scanned handler set (internal) separate from the exposed set. When a request names a scanned but non-exposed handler, the controller returns the same plain 404 (no hint in the body) and logs a Warning, `Tale handler [{HandlerName}] exists but is not exposed over HTTP; call ExposeOverHttp<THandler>() to expose it.`. That gives operators of upgraded apps a server-side signal for P-12. Test: `Start_ScannedButNotExposed_Returns404AndLogsWarning`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green.
- [ ] The DreamTravel UI flow tests are green (`dotnet test sample-tale-code-apps/DreamTravel/tests/Unit/DreamTravel.Ui.UnitTests`, component tests).
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [ ] RM-14: `Start_ScannedButNotExposed_Returns404AndLogsWarning` is green, and the 404 body does not name the handler or `ExposeOverHttp`.

## Open questions
- none (`C-06`, `C-07` confirmed by the user on 2026-11-02)

## Deviations
