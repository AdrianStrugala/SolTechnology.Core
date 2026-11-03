---
spec: 2026-11-02-core-2-0-logging
step: 02
status: done
---

# Step 02: Logging — `AddSolLogging(IConfiguration)` registers its middleware dependencies

## Summary
The configuration overload of `AddSolLogging` is the one `docs/Log.md:44,81` recommends. It
registers neither `ITimingService` nor `TimeProvider`, so `LoggingMiddleware` fails on every
request (`B4`, `src/SolTechnology.Core.Logging/ModuleInstaller.cs:53`,
`LoggingMiddleware.cs:33`). Both overloads must share one registration path.

Former core step 14; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Logging/ModuleInstaller.cs` — EDIT — private `RegisterCore()` called by both overloads
- `tests/SolTechnology.Core.Logging.Tests/ModuleResolutionTests.cs` — EDIT (created in step 01) — remove the B4 ignore
- `tests/SolTechnology.Core.Logging.Tests/SolTechnology.Core.Logging.Tests.csproj` — EDIT — `PackageReference Microsoft.AspNetCore.TestHost 10.0.9` (canonical version) for `TestServer`

## Changes
- Move the `TryAdd*` registrations shared by both overloads (`ICorrelationIdService`, `ITimingService`, `TimeProvider.System`, enrichers) into one private method.
- No change to option binding (`LoggingOptions.SectionName`).
- `Logging/ModuleInstaller.cs` is already 167 lines; keep the net growth minimal. Step 07 brings it under 150.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green with no `[Ignore]`.
- [x] A `TestServer` request through `UseSolLogging()` after `AddSolLogging(configuration)` returns 200 (added to the same test class).

## Open questions
- none

## Deviations

### Shared path also owns option validation

**Original plan:** Move the shared `TryAdd*` registrations (`ICorrelationIdService`, `ITimingService`, `TimeProvider.System`, enrichers) into a private `RegisterCore()`.

**Actual implementation:** `RegisterCore()` also creates the `LoggingOptions` builder with the validation chain that both overloads duplicated, and returns it; the `Action` overload adds `Configure`, the configuration overload adds `Bind`. Option binding and validation rules are unchanged, and `ModuleInstaller.cs` shrinks from 167 to 161 lines instead of growing. The `Microsoft.AspNetCore.TestHost` consumer list in the package-management canonical-version table now includes `SolTechnology.Core.Logging.Tests`.
