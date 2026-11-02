---
spec: 2026-11-02-core-2-0-cache
step: 07
status: to-do
---

# Step 07: Guide conformance — Cache

## Summary
Cache services hand-write constructors (`SingletonCache.cs:16`, `RedisDistributedLockService.cs:19`,
`RedisIdempotencyStore.cs:31`, `LocalIdempotencyStore.cs:15`), and `RedisHealthCheckExtensions.cs:22`
uses `static class` + `this` (`logging-cache#21,26`). This is a behaviour-preserving refactor.

Former core step 67 (Cache part); split per library on 2026-11-02. The Cache log templates are step 08.

## Affected components
- `src/SolTechnology.Core.Cache/SingletonCache.cs`, `RedisDistributedLockService.cs`, `RedisIdempotencyStore.cs`, `LocalIdempotencyStore.cs` — EDIT — primary constructors
- `src/SolTechnology.Core.Cache/HealthChecks/RedisHealthCheckExtensions.cs` — EDIT — `extension` blocks, braces

## Changes
- No public or protected signature change; no log-template change (that is step 08).
- Behaviour-preserving refactor; use the [`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.
- Premortem RM-16 (core step 00, 2026-11-02, scenario 16): the Redis service primary constructors must not change the Redis key composition. These guard tests keep their names and assertions:
  - Cache (step 03): `RedisKeys_LockAndIdempotency_KeepInstanceNamePrefixFormat`, `CacheKey_GenericItemType_HasNoAssemblyVersion`.

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.Cache -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Cache.Tests` is green.
- [ ] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [ ] RM-16: `git diff --stat -- tests/SolTechnology.Core.Cache.Tests` for this step prints nothing; the guard tests above are green.

## Open questions
- none

## Deviations
