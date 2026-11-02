---
spec: 2026-11-02-core-2-0-cache
step: 03
status: to-do
---

# Step 03: Cache — type-namespaced keys, validated non-empty `InstanceName`, validated options

## Summary
Cache keys and configuration allow silent collisions:

- Keys are only the JSON of `key`, so call sites with the same key share data. In Redis, a
  different `TItem` can be deserialized into the wrong type (`logging-cache#8`,
  `SingletonCache.cs:24`, `RedisCache.cs:27,36`).
- The default `InstanceName = "SolTechnology:"` is shared by every app on a Redis instance
  (`logging-cache#9`).
- `ValidateOnStart()` has no validators, and `AddStackExchangeRedisCache` opens a second
  multiplexer (`logging-cache#24`).

This is a persisted-contract change (P-07): existing Redis entries are orphaned.

Former core step 29; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Cache/SingletonCache.cs` — EDIT — key prefix built from version-free type names of `TKey` and `TItem` (premortem RM-3; not `Type.FullName`, which embeds assembly versions for generic arguments)
- `src/SolTechnology.Core.Cache/RedisCache.cs` — EDIT — same prefix
- `src/SolTechnology.Core.Cache/DistributedCacheConfiguration.cs` — EDIT — `:6` `InstanceName` default `"SolTechnology:"` removed (becomes `string.Empty`); no `required` modifier, because that would be a public API change no `C-nn` covers; the installer validates it instead
- `src/SolTechnology.Core.Cache/CacheConfiguration.cs` — EDIT — only if the validators need attributes
- `src/SolTechnology.Core.Cache/ModuleInstaller.cs` — EDIT — `:26,47,54` `.Validate(o => o.ExpirationSeconds > 0)`, `.Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString))`, `.Validate(o => !string.IsNullOrWhiteSpace(o.InstanceName))`; `options.ConnectionMultiplexerFactory` reuses the singleton
- `tests/SolTechnology.Core.Cache.Tests/` — EDIT — key isolation and validation tests (`RedisFixture`)
- `tests/SolTechnology.Core.Cache.Tests/SolTechnology.Core.Cache.Tests.csproj` — EDIT — `ProjectReference` to `src/SolTechnology.Core.Redis.Testing`
- `sample-tale-code-apps/DreamTravel/**/appsettings*.json` — EDIT — set `InstanceName` where the distributed cache is configured

## Changes
- Tests:
  - `GetOrAdd_SameKeyDifferentItemType_DoesNotCollide`.
  - `AddSolDistributedCache_MissingInstanceName_HostStartFails`.
  - `AddSolDistributedCache_UsesSingleMultiplexer`.
- Premortem mitigations (core step 00, 2026-11-02):
  - RM-2 (scenario 2): `RedisDistributedLockService.cs:26` and `RedisIdempotencyStore.cs:39` prefix their keys with `InstanceName`, so removing the default also moves the lock and idempotency keyspace.
    - Amend the core brief's P-07 row: lock (`{InstanceName}lock:{name}`) and idempotency (`{InstanceName}idempotency:{key}`) keys derive from `InstanceName`. `InstanceName = "SolTechnology:"` keeps the 1.x lock and idempotency keys. Any other value makes 1.x and 2.x instances miss each other's locks and idempotency records during the rollout.
    - Keep the composition of both keys unchanged; only the cache keys get the type prefix.
    - Test `RedisKeys_LockAndIdempotency_KeepInstanceNamePrefixFormat` (`RedisFixture`) asserts both raw Redis key formats.
  - RM-3 (scenario 3): a single internal helper builds the version-free type name (for example from `Type.Namespace` and `Type.Name`, recursing into generic arguments without assembly information). Test `CacheKey_GenericItemType_HasNoAssemblyVersion` uses `List<SomeTestDto>` and asserts that the key contains none of `Version=`, `Culture=`, or `PublicKeyToken=`.
  - RM-13 (scenario 13): append a `P-nn` row to the core brief for `CacheConfiguration.ExpirationSeconds > 0` (start-time validation). `CacheConfiguration.ExpirationSeconds` defaults to `0`, and `AddSolLocalCache(config)` copies the object as is (`Cache/ModuleInstaller.cs:20-26`). Choose whether a partial config without seconds falls back to the 20-minute default or fails at start, pin it with `AddSolLocalCache_ConfigWithoutExpirationSeconds_<ChosenOutcome>`, and state it in the row.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Cache.Tests` is green (Docker).
- [ ] The DreamTravel build and component tests are green.
- [ ] The core brief's P-07 row matches the delivered behaviour; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.
- [ ] `git diff` of `DistributedCacheConfiguration.cs` adds no `required` keyword.
- [ ] Premortem mitigations for `ModuleInstaller.cs` and the persisted contract are applied.
- [ ] RM-2: `RedisKeys_LockAndIdempotency_KeepInstanceNamePrefixFormat` is green, and the core brief's P-07 row names the lock and idempotency keyspace and the `SolTechnology:` continuity value.
- [ ] RM-3: `CacheKey_GenericItemType_HasNoAssemblyVersion` is green.
- [ ] RM-13: the core brief's P-table has the `ExpirationSeconds` validation row, and the partial-config test is green.

## Open questions
- none

## Deviations
