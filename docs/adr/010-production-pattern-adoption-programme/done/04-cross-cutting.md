---
adr: 010-production-pattern-adoption-programme
step: 04 of 07
status: done
---
# Step 04: Cross-cutting — `ValidateOnStart` + `TimeProvider` + coding-guide rules

## Summary
Wire `ValidateOnStart` in all modules, replace `DateTime.UtcNow` with `TimeProvider`, update coding guide.

## Affected components
- `src/SolTechnology.Core.Cache/ModuleInstaller.cs` — `.ValidateDataAnnotations().ValidateOnStart()` (G3)
- `src/SolTechnology.Core.SQL/ModuleInstaller.cs` — same (G3)
- `src/SolTechnology.Core.MessageBus/ModuleInstaller.cs` — same (G3)
- `src/SolTechnology.Core.BlobStorage/ModuleInstaller.cs` — same (G3)
- `src/SolTechnology.Core.Authentication/ModuleInstaller.cs` — same (G3)
- `src/SolTechnology.Core.Scheduler/ModuleInstaller.cs` — same (G3)
- `src/SolTechnology.Core.Api/ModuleInstaller.cs` — same (G3)
- `src/SolTechnology.Core.Logging/ModuleInstaller.cs` — add `.ValidateOnStart()` (G3)
- `src/SolTechnology.Core.AUID/Auid.cs` — inject `TimeProvider` (G1)
- `src/SolTechnology.Core.Story/Orchestration/StoryEngine.cs` — inject `TimeProvider` (G1)
- `docs/ClaudeCodingGuide.md` — G2 (static JsonSerializerOptions), G6 ([ExcludeFromCodeCoverage]), G7 (primary-ctor), logging guard-rail

## Details
- **G3:** Behaviour change — bad config = host won't start.
- **G1:** Replace `DateTime.UtcNow` with `timeProvider.GetUtcNow()` in AUID + Story.
- **G2/G6/G7:** Documentation rules in coding guide.

## Acceptance criteria
- All modules with `AddOptions<T>` have `ValidateOnStart`
- No `DateTime.UtcNow` in `src/` (except test infra)
- Tests pass; `dotnet build` green

## Open questions
- none

