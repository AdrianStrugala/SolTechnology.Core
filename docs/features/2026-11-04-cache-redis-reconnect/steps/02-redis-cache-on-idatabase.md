---
spec: 2026-11-04-cache-redis-reconnect
step: 02
status: to-do
---

# Step 02: `IRedisCache` on `IDatabase`; drop `IDistributedCache` and its package

## Summary
`RedisCache` stops using Microsoft's `IDistributedCache` and reads and writes Redis strings through
`RedisConnectionProvider.GetDatabase()` per call. Keys stay byte-identical, the absolute `ExpirationSeconds` TTL and
the fall-through on failure stay, and the value becomes a string instead of a hash (decision B in the brief, P-22).
`AddSolDistributedCache` no longer calls `AddStackExchangeRedisCache`, so `Microsoft.Extensions.Caching.StackExchangeRedis`
leaves the package (P-21). This must land before step 03, because Microsoft's `RedisCache` keeps and disposes the
multiplexer it was given.

## Affected components
- `tests/SolTechnology.Core.Cache.Tests/Redis/RedisCacheTests.cs` — EDIT — layout-independent pinning tests first (commit 1), layout tests with the switch (commit 2)
- `src/SolTechnology.Core.Cache/RedisCache.cs` — EDIT — `RedisCache(RedisConnectionProvider connection, IOptions<DistributedCacheConfiguration> options, ILogger<RedisCache> logger)`
- `src/SolTechnology.Core.Cache/ModuleInstaller.cs` — EDIT — remove `using Microsoft.Extensions.Caching.StackExchangeRedis;`, `services.AddStackExchangeRedisCache(...)`, and the `AddOptions<RedisCacheOptions>()` block; XML doc of `AddSolDistributedCache` drops "the framework `IDistributedCache` for Redis"
- `src/SolTechnology.Core.Cache/SolTechnology.Core.Cache.csproj` — EDIT — remove `<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" />`
- `Directory.Packages.props` — EDIT — remove `<PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.1" />` (line 29; no other project references it)
- `.github/skills/package-management/references/canonical-versions.md` — EDIT — remove the `Microsoft.Extensions.Caching.StackExchangeRedis` row (line 31)
- `tests/SolTechnology.Core.Cache.Tests/Redis/DistributedCacheRegistrationTests.cs` — DELETE — it tested the `IDistributedCache` reuse that no longer exists
- `tests/SolTechnology.Core.Cache.Tests/ModuleResolutionTests.cs` — EDIT — stop resolving `IDistributedCache`; assert `provider.GetService<IDistributedCache>()` is `null`

## Changes
- Full key: `(RedisKey)(_config.InstanceName + CacheKey.For<TKey, TItem>(key, SerializerOptions))` — byte-identical to Microsoft's `_instancePrefix.Append(key)` (UTF-8 of `InstanceName` followed by UTF-8 of the key).
- Read: `connection.GetDatabase().StringGetAsync(fullKey)`; when the value is not null or empty, `JsonSerializer.Deserialize<TItem>((byte[])value!, SerializerOptions)`; a non-null result is returned, otherwise the factory runs (as today).
- Write: `connection.GetDatabase().StringSetAsync(fullKey, JsonSerializer.SerializeToUtf8Bytes(result, SerializerOptions), TimeSpan.FromSeconds(_config.ExpirationSeconds))` — one `SET … EX` command. `GetDatabase()` is called again for the write; no `IDatabase` is held across the factory call, which may outlast the step 03 grace period.
- Log templates and the `[{CacheKey}]` property value (the key without `InstanceName`, as today) are unchanged: `Distributed cache GET failed for key [{CacheKey}] — falling through to factory`, `Distributed cache SET failed for key [{CacheKey}] — skipping`.
- `SerializerOptions` (camelCase) are unchanged.
- No public symbol changes. DI contract change: `IDistributedCache` and `IOptions<RedisCacheOptions>` are no longer registered by `AddSolDistributedCache` (P-21).

## Acceptance criteria
- [ ] Commit 1 (before the switch) adds and passes against the current `IDistributedCache` implementation:
  - `GetOrAdd_Hit_DoesNotCallFactory` — second call returns the first value; factory called once.
  - `GetOrAdd_Miss_SetsTtlWithinExpirationSeconds` — `KeyTimeToLive` of the raw key is in `(0, 300]` s (default `ExpirationSeconds`).
  - `GetOrAdd_JsonNullCached_CallsFactory` — a cached JSON `null` (factory returned `null` for a reference type) makes the next call run the factory again.
- [ ] Commit 2 (the switch) adds and passes:
  - `GetOrAdd_Miss_StoresCamelCaseJsonString` — `KeyType` of the raw key is `RedisType.String`; the raw value equals `{"name":"Wroclaw","population":640000}` for `SomeTestDto("Wroclaw", 640_000)`.
  - `GetOrAdd_KeyHoldsHash_FallsThroughAndOverwrites` — pre-seed `HSET` `data` under the exact full key; the first call returns the factory result and logs the GET Warning (captured by a test-local `ServiceCollection` that registers a `CapturingLogger<RedisCache>` as `ILogger<RedisCache>`); the second call returns the cached value without calling the factory; the key type is now `String`.
- [ ] `CacheKey_GenericItemType_HasNoAssemblyVersion` and `GetOrAdd_SameKeyDifferentItemType_DoesNotCollide` pass unchanged.
- [ ] `ModuleInstallerTests.AddSolDistributedCache_RedisDownAtStart_FailsOpen` passes unchanged (fall-through when Redis is down).
- [ ] `grep -rn "IDistributedCache\|Caching.StackExchangeRedis\|RedisCacheOptions" src/SolTechnology.Core.Cache Directory.Packages.props` prints nothing.
- [ ] `dotnet list src/SolTechnology.Core.Cache package --include-transitive | grep -i "Caching.StackExchangeRedis"` prints nothing.
- [ ] `dotnet pack src/SolTechnology.Core.Cache -c Release -o .scratch/pack-cache-reconnect` succeeds and `unzip -p .scratch/pack-cache-reconnect/SolTechnology.Core.Cache.2.0.0.nupkg SolTechnology.Core.Cache.nuspec | grep -c "Caching.StackExchangeRedis"` prints `0`.
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` reports 0 errors; `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Cache.Tests` is green with Docker running.

## Open questions
- none

## Deviations
