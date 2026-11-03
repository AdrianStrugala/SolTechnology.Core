---
spec: 2026-11-02-core-2-0-logging
step: 07
status: in-progress
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
- [ ] `find src/SolTechnology.Core.Logging -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [ ] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.Logging --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [ ] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations
