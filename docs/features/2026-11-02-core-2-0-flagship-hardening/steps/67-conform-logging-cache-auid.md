---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 67
status: to-do
---

# Step 67: Guide conformance — Logging, Cache, AUID

## Summary
Five Logging and AUID files exceed the size caps (`logging-cache#19,21,26`):

- `LoggingMiddleware.cs` has 269 lines, and `InvokeAsync` alone is about 120.
- `Auid.cs` has 460 lines, and `TryParse` is about 45.
- `Logging/ModuleInstaller.cs` (167), `Correlations/CorrelationId.cs` (164), and
  `Enrichment/LogDetailEnricher.cs` (155) at `4097d47f`; steps 14, 30, and 31 make them longer
  first.

The step also fixes:

- Hand-written constructors in `SingletonCache.cs:16`, `RedisDistributedLockService.cs:19`,
  `RedisIdempotencyStore.cs:31`, `LocalIdempotencyStore.cs:15`, `TimingService.cs:15`,
  `RequestHeadersEnricher.cs:26`, and `LogDetailEnricher.cs:18`.
- `static class` + `this` in `LoggerScopeExtensions.cs:10`, `Logging/ModuleInstaller.cs:142`, and
  `RedisHealthCheckExtensions.cs:22`.
- The unbraced `foreach` at `LoggerScopeExtensions.cs:21`.
- Logging test conventions (`ModuleInstallerTests.cs:13`: snake_case names, no AAA markers).

## Affected components
- `src/SolTechnology.Core.Logging/Middleware/LoggingMiddleware.cs` — EDIT — extract `RequestBodyBuffer.cs`, `LoggingPathMatcher.cs`, `RequestEnvelopeLogger.cs` (NEW, internal, `[LoggerMessage]`)
- `src/SolTechnology.Core.AUID/Auid.cs` — EDIT — partials `Auid.Format.cs`, `Auid.Parse.cs`, `Auid.Encoding.cs` (NEW)
- `src/SolTechnology.Core.Cache/SingletonCache.cs`, `RedisDistributedLockService.cs`, `RedisIdempotencyStore.cs`, `LocalIdempotencyStore.cs` — EDIT — primary constructors
- `src/SolTechnology.Core.Logging/TimingService.cs`, `Enrichment/RequestHeadersEnricher.cs`, `Enrichment/LogDetailEnricher.cs` — EDIT — primary constructors
- `src/SolTechnology.Core.Logging/Extensions/LoggerScopeExtensions.cs`, `src/SolTechnology.Core.Cache/HealthChecks/RedisHealthCheckExtensions.cs` — EDIT — `extension` blocks, braces
- `src/SolTechnology.Core.Logging/ModuleInstaller.cs` — EDIT — `extension` block; below 150 lines (move the shared `RegisterCore()` from step 14 and the enricher registrations into an internal `LoggingServiceRegistrations.cs`, NEW)
- `src/SolTechnology.Core.Logging/Correlations/CorrelationId.cs` — EDIT — below 150 lines (extract the inbound-ID validation from step 31 into an internal `CorrelationIdValidator.cs`, NEW)
- `src/SolTechnology.Core.Logging/Enrichment/LogDetailEnricher.cs` — EDIT — below 150 lines (reuse `LoggingPathMatcher` from the middleware split)
- `tests/SolTechnology.Core.Logging.Tests/ModuleInstallerTests.cs` — EDIT — `Method_Scenario_ExpectedOutcome` names, `// Arrange/Act/Assert`

## Changes
- No public or protected signature change; no log-template change.
- Premortem RM-16 (step 00, 2026-11-02, scenario 16): the `Auid` partial split and the Redis service primary constructors must not change the AUID string format, the converter behaviour, or the Redis key composition. These guard tests keep their names and assertions:
  - AUID (steps 19, 35): `ToString_UnderCulture_RoundTrips`, `TryParse_OutOfRange_ReturnsFalse`, `TryParse_1xThaiCalendarString_ReturnsFalse`, `Read_Empty_ThrowsJsonException`, `RoundTrip_DictionaryKey`;
  - Cache (step 29): `RedisKeys_LockAndIdempotency_KeepInstanceNamePrefixFormat`, `CacheKey_GenericItemType_HasNoAssemblyVersion`.

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.Logging src/SolTechnology.Core.Cache src/SolTechnology.Core.AUID -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `dotnet test` green for Logging, Cache, AUID test projects.
- [ ] Public API diff is empty.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [ ] RM-16: `git diff --stat -- tests/SolTechnology.Core.AUID.Tests tests/SolTechnology.Core.Cache.Tests` for this step prints nothing; the guard tests above are green.

## Open questions
- none

## Deviations
