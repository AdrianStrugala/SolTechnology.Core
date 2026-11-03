---
spec: 2026-11-02-core-2-0-logging
step: 07
status: done
---

# Step 07: Guide conformance — Logging

## Summary
Four Logging files exceed the size caps (`logging-cache#19,21,26`):

- `LoggingMiddleware.cs` has 269 lines, and `InvokeAsync` alone is about 120.
- `Logging/ModuleInstaller.cs` (167), `Correlations/CorrelationId.cs` (164), and
  `Enrichment/LogDetailEnricher.cs` (155) at `4097d47f`; steps 02, 03, and 04 make them longer
  first.

The step also fixes hand-written constructors in `TimingService.cs:15`,
`RequestHeadersEnricher.cs:26`, and `LogDetailEnricher.cs:18`; `static class` + `this` in
`LoggerScopeExtensions.cs:10` and `Logging/ModuleInstaller.cs:142`; the unbraced `foreach` at
`LoggerScopeExtensions.cs:21`; Logging test conventions (`ModuleInstallerTests.cs:13`: snake_case
names, no AAA markers); and a stale MediatR comment (`logging-cache#26`).

Former core steps 67 (Logging part) and 66 (`LogScopeAttribute` comment) plus the Logging-scoped part of former step 70; split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.Logging/Middleware/LoggingMiddleware.cs` — EDIT — extract `RequestBodyBuffer.cs`, `LoggingPathMatcher.cs`, `RequestEnvelopeLogger.cs` (NEW, internal, `[LoggerMessage]`)
- `src/SolTechnology.Core.Logging/TimingService.cs`, `Enrichment/RequestHeadersEnricher.cs`, `Enrichment/LogDetailEnricher.cs` — EDIT — primary constructors
- `src/SolTechnology.Core.Logging/Extensions/LoggerScopeExtensions.cs` — EDIT — `extension` blocks, braces
- `src/SolTechnology.Core.Logging/ModuleInstaller.cs` — EDIT — `extension` block; below 150 lines (move the shared `RegisterCore()` from step 02 and the enricher registrations into an internal `LoggingServiceRegistrations.cs`, NEW)
- `src/SolTechnology.Core.Logging/Correlations/CorrelationId.cs` — EDIT — below 150 lines (extract the inbound-ID validation from step 04 into an internal `CorrelationIdValidator.cs`, NEW)
- `src/SolTechnology.Core.Logging/Enrichment/LogDetailEnricher.cs` — EDIT — below 150 lines (reuse `LoggingPathMatcher` from the middleware split)
- `src/SolTechnology.Core.Logging/LogScopeAttribute.cs` — EDIT — `:5` remove the MediatR comment
- `tests/SolTechnology.Core.Logging.Tests/ModuleInstallerTests.cs` — EDIT — `Method_Scenario_ExpectedOutcome` names, `// Arrange/Act/Assert`

## Changes
- No public or protected signature change; no log-template change.
- Behaviour-preserving refactor; use the [`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.
- Log templates: the scoped `[{Placeholder}]` check below is this library's part of former step 70 (L-03). Fix any unbracketed placeholder it finds and add the property name to the core brief's L-03 entry.

## Acceptance criteria
- [x] `find src/SolTechnology.Core.Logging -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [x] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.Logging --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [x] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations

### Extraction set and commit shape

**Original plan:** Extract `RequestBodyBuffer.cs`, `LoggingPathMatcher.cs` and `RequestEnvelopeLogger.cs` from `LoggingMiddleware.cs`; `LogDetailEnricher.cs` reuses the matcher; `CorrelationId.cs` drops below 150 by extracting `CorrelationIdValidator.cs`; primary constructors in `TimingService`, `RequestHeadersEnricher` and `LogDetailEnricher`.

**Actual implementation:** `LoggingPathMatcher` already existed from step 03, which also brought `LogDetailEnricher` below 150. Following the refactor skill, three pin-test files landed before the moves they guard, in new files so the test files of steps 01–05 have no diff (premortem RM-16): `Middleware/LoggingMiddlewareBodyBufferingTests.cs`, `Middleware/LoggingMiddlewareEnvelopeTests.cs` (exact templates and levels via `{OriginalFormat}`) and `Correlations/CorrelationIdTraceParentTests.cs`. Each smell is its own commit. `RequestEnvelopeLogger` also owns the status-code-to-level choice for the Finished entry and the body-parse Debug entry, and its `[LoggerMessage]` entries carry event ids 2141–2148 (previously 0); templates and levels are unchanged. Extracting `CorrelationIdValidator` alone left `CorrelationId.cs` at 163 lines, so the private traceparent guard became one expression and the essay in the `TryBuildTraceParentFromCurrentActivity` summary was cut (146 lines). `LoggingMiddleware` also got a primary constructor (same public signature) and an accurate summary (it does read the body for `IRequiresRequestBody` enrichers); the stale MediatR wording was also removed from `OperationLogMessages.cs` and `CoreLoggingActivitySources.cs`. `ModuleInstallerTests` merged its two invalid-option tests into one parameterised test.

### Verification details

**Original plan:** Placeholder check with the given regex (tune it); public API diff via ref assemblies or `Microsoft.DotNet.ApiCompat`; apply the premortem mitigations for `ModuleInstaller.cs`.

**Actual implementation:** The given regex cannot see `[LoggerMessage]` templates (the `Message =` line has no `Log…(`), so it was also run as `grep -rnE "\"[^\"]*[^\[]\{[@A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.Logging --include=*.cs | grep -v "///"`; both print nothing, so the core brief's L-03 entry needs no Logging property. `Microsoft.DotNet.ApiCompat.Tool` 10.0.403 compared the Release `SolTechnology.Core.Logging.dll` before step 07 (`6e456ec4`) and after: no breaking changes with parameter-name and attribute rules enabled; strict mode lists only the two compiler-generated extension marker types (`LoggerScopeExtensions.<G>$…`, `ModuleInstaller.<G>$…`) that C# 14 `extension` blocks emit, which consumers cannot reference. The only premortem mitigation that applies to this step is RM-16: `git diff --stat 6e456ec4 HEAD -- tests/` shows only the three new pin files and the planned `ModuleInstallerTests.cs` edit.
