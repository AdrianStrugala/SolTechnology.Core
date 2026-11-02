---
spec: 2026-11-02-core-2-0-api-auth
step: 04
status: to-do
---

# Step 04: Api — keep 201/202 in `ResultConversionFilter`; real filter registration

## Summary
`ResultConversionFilter` replaces any `ObjectResult` holding a successful `Result<T>` with a new
200 result. `CreatedAtAction(…, result)` and `Accepted(result)` therefore lose their status code
and `Location` header (`http-api#7`, `Filters/ResultConversionFilter.cs:46-49,72`). The
`AddScoped<ExceptionFilter/ResultConversionFilter>()` registrations are dead, because
`Filters.Add<T>()` builds the filter through `ActivatorUtilities` (`http-api#19`). One XML doc
contradicts the code (`http-api#22`, `ApiExceptionOptions.cs:12-13`).

Former core step 22; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.API/Filters/ResultConversionFilter.cs` — EDIT — on 2xx, set `objectResult.Value = data` and keep the status and type
- `src/SolTechnology.Core.API/ApiCoreMvcOptionsExtensions.cs` — EDIT — `:42-43` `options.Filters.AddService<T>()`
- `src/SolTechnology.Core.API/ModuleInstaller.cs` — EDIT — `:157-158` registrations kept (now used)
- `src/SolTechnology.Core.API/Exceptions/ApiExceptionOptions.cs` — EDIT — `:12-13` doc says `Extensions["exception"]`
- `tests/SolTechnology.Core.API.Tests/ResultConversionFilterTests.cs` — EDIT

## Changes
- Tests:
  - `CreatedAtAction_WithSuccessfulResult_Keeps201AndLocation`.
  - `Accepted_WithSuccessfulResult_Keeps202`.
  - `Ok_WithFailedResult_MapsToProblemDetails` (regression).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.API.Tests` is green, and the 201 test fails on `4097d47f`.
- [ ] The DreamTravel component tests are green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations
