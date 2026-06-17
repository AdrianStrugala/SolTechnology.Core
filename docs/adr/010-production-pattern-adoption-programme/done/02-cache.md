---
adr: 010-production-pattern-adoption-programme
step: 02 of 07
status: done
---
# Step 02: Cache — distributed tier + resilient cache-aside + decorator + invalidator

## Summary
Add `IDistributedTaskCache` (Redis-backed), fail-open degradation, Scrutor decorator helper, and cache-key/invalidator.

## Affected components
- `src/SolTechnology.Core.Cache/Distributed/IDistributedTaskCache.cs` — new (C1)
- `src/SolTechnology.Core.Cache/Distributed/DistributedTaskCache.cs` — new (C1+C2)
- `src/SolTechnology.Core.Cache/Extensions/ServiceCollectionExtensions.cs` — new (C3)
- `src/SolTechnology.Core.Cache/Keys/CacheKey.cs` — new (C4)
- `src/SolTechnology.Core.Cache/Invalidation/ICacheInvalidator.cs` — new (C4)
- `src/SolTechnology.Core.Cache/Invalidation/DistributedCacheInvalidator.cs` — new (C4)
- `src/SolTechnology.Core.Cache/Invalidation/NoCacheInvalidator.cs` — new (C4)
- `src/SolTechnology.Core.Cache/DistributedCacheConfiguration.cs` — new
- `src/SolTechnology.Core.Cache/ModuleInstaller.cs` — add `AddDistributedCache()`
- `src/SolTechnology.Core.Cache/SolTechnology.Core.Cache.csproj` — add `Microsoft.Extensions.Caching.StackExchangeRedis` + `Scrutor`

## Details
- **C1:** `GetAsync<T>`, `SetAsync<T>`, `RemoveAsync`, `ExistsAsync`. Static `JsonSerializerOptions` (G2). Default TTL from config.
- **C2:** All Redis I/O wrapped in try/catch → `LogWarning` → return null/no-op. No Polly retry.
- **C3:** `services.AddCachedDecorator<IFoo, CachedFoo>()` via Scrutor `Decorate<,>()`.
- **C4:** `CacheKey.For<T>(params object[])` → `"TypeName:part1:part2"`. `ICacheInvalidator` with `NoCacheInvalidator` no-op default.

## Acceptance criteria
- `AddDistributedCache(config)` registers all services; existing `AddCache()` unchanged
- Redis failure → null return + warning log (not exception)
- `CacheKey.For<MyQuery>(42)` → `"MyQuery:42"`
- Tests pass; `dotnet build` green

## Open questions
- none


