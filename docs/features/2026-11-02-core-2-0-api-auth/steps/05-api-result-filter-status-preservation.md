---
spec: 2026-11-02-core-2-0-api-auth
step: 05
status: done
---

# Step 05: Api — keep 201/202 in `ResultConversionFilter`; real filter registration

## Summary
`ResultConversionFilter` replaces any `ObjectResult` holding a successful `Result<T>` with a new
200 result. `CreatedAtAction(…, result)` and `Accepted(result)` therefore lose their status code
and `Location` header (`http-api#7`, `Filters/ResultConversionFilter.cs:46-49,72`). The
`AddScoped<ExceptionFilter/ResultConversionFilter>()` registrations are dead, because
`Filters.Add<T>()` builds the filter through `ActivatorUtilities` (`http-api#19`). One XML doc
contradicts the code (`http-api#22`, `ApiExceptionOptions.cs:12-13`).

Former core step 22; moved here by the per-library regroup (user, 2026-11-02). Renumbered from 04 on 2026-11-03 (new step 01, `C-28`).

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
- [x] `dotnet test tests/SolTechnology.Core.API.Tests` is green, and the 201 test fails on `4097d47f`.
- [x] The DreamTravel component tests are green.
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations

### The filter edits the action's result in place and clears its declared type

**Original plan:** on 2xx, set `objectResult.Value = data` and keep the status and type.

**Actual implementation:** a successful result inside a non-200 2xx result (`CreatedAtActionResult`, `AcceptedResult`)
keeps its type, status, route values and `Location`; a 200 or status-less result is also edited in place (status 200)
instead of being replaced, and a null payload still becomes 204. Editing in place would have kept
`ObjectResult.DeclaredType`, which is `Result<T>` for an `ActionResult<Result<T>>` action, so the formatter would have
serialized the payload as `Result<T>`; the filter sets `DeclaredType = null`, pinned by the extra test
`GenericResultSuccess_FromActionResultOfT_SerializesPayloadType` (it fails when that line is removed). A second extra test,
`ApiCoreMvcOptionsExtensionsTests.AddSolApiCoreFilters_RegistersFiltersResolvedFromDI`, pins the `AddService<T>()`
registration; it and the 201/202 tests failed before the fix (the two files are unchanged since `4097d47f`).
`ModuleInstaller.cs` needed no edit: the scoped registrations are now the ones MVC resolves, so lifetimes and the
premortem `di.md` checklist are unaffected.

