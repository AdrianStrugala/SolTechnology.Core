---
spec: 2026-07-06-opentelemetry-support
step: 05
status: to-do
---

# Step 05: Cache telemetry

## Summary

Adds hit/miss counters to all three cache tiers so cache effectiveness becomes a
dashboardable number. Metrics only — Redis command spans remain a consumer opt-in via
the vendor instrumentation package, which is documented but not referenced.

## Affected components

- `src/SolTechnology.Core.Cache/Telemetry/CacheMetrics.cs` — NEW — meter (shape of `HttpClientMetrics`)
- `src/SolTechnology.Core.Cache/SingletonCache.cs` — EDIT — hit/miss increments
- `src/SolTechnology.Core.Cache/ScopedCache.cs` — EDIT — hit/miss increments
- `src/SolTechnology.Core.Cache/RedisCache.cs` — EDIT — hit/miss increments
- `src/SolTechnology.Core.Cache/ModuleInstaller.cs` — EDIT — `TryAddSingleton` in both installers
- `src/SolTechnology.Core.Cache/SolTechnology.Core.Cache.csproj` — EDIT — minor version bump
- `tests/SolTechnology.Core.Cache.Tests` — EDIT — metric tests

## Changes

- NEW `CacheMetrics` (stable contract — MAJOR bump to change):
  - `Meter` name `SolTechnology.Core.Cache` via `IMeterFactory`.
  - `Counter<long> soltechnology.core.cache.hits` — tag `cache.type` = `singleton` |
    `scoped` | `redis`.
  - `Counter<long> soltechnology.core.cache.misses` — same tag.
  - NEVER tag the cache key (unbounded cardinality).
- EDIT the three cache classes: hit = stored value returned; miss = factory/fallback
  executed. Constructor additions are premortem-covered (public, DI-constructed types).
- EDIT `ModuleInstaller`: `services.TryAddSingleton<CacheMetrics>()` in both the local
  and the distributed installer paths.
- csproj: minor version bump.
- Tests: `MetricCollector<long>` — one parameterized fixture asserting hit and miss
  counts per `cache.type` (`[TestCase("singleton")]`, `[TestCase("scoped")]`,
  `[TestCase("redis")]` where setup allows; Redis tier via the existing
  `SolTechnology.Core.Redis.Testing` fixture).

## Acceptance criteria

- [ ] `dotnet build SolTechnology.Core.slnx` green.
- [ ] `dotnet test tests/SolTechnology.Core.Cache.Tests` green.
- [ ] No instrument tag carries a cache key (asserted in test via collected tag names).

## Open questions

- none

## Deviations

<!-- Empty at authoring time. Filled by implement-plan when reality diverges from the plan. -->
