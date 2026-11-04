---
spec: 2026-11-02-core-2-0-cache
step: 01
status: done
---

# Step 01: DI resolution test — Cache

## Summary
Existing installer tests only check service descriptors, so they missed resolution failures `B1`, `B2`, and `B4` (`P1-2`). This step adds the Cache resolution test. It builds the provider with `ValidateOnBuild = true` and `ValidateScopes = true`, then resolves every public service the module registers. Known-broken registrations land as `[Ignore]`d tests that cite the finding ID, and the later step of this feature that fixes each one removes the `Ignore`.

Former core step 11 (Cache part); split per library on 2026-11-02.

## Affected components
- `tests/SolTechnology.Core.Cache.Tests/ModuleResolutionTests.cs` — NEW

## Changes
- Pattern (NUnit, FluentAssertions, `// Arrange/Act/Assert`, name `AddSol<Module>_ResolvesAllPublicServices_WithValidation`):
  - `new ServiceCollection().AddLogging()`, then the module's `AddSol*` overloads.
  - `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })`.
  - Resolve every public service type inside `CreateScope()`.
- Dummy connection strings are enough because resolution must not open connections; record a deviation if one does.
- Coverage: `ISingletonCache`, `IScopedCache<,>`, local and distributed lock/idempotency registrations.
- Follow the [`test-writing`](../../../../.github/skills/test-writing/SKILL.md) skill. No shared helper in a shipped package.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Cache.Tests` is green, with no ignored resolution test.

## Open questions
- none

## Deviations

### Distributed resolution starts a background connect

**Original plan:** dummy connection strings are enough because resolution must not open connections.

**Actual implementation:** `AddSolDistributedCache` registers `IConnectionMultiplexer` through the eager
`ConnectionMultiplexer.Connect`, so resolving it, and the Redis lock and idempotency store that depend on it, starts a
connection attempt. The test uses `localhost:1,abortConnect=false,connectTimeout=100`, so resolution succeeds without a
server. Two tests cover the local set (`ISingletonCache`, `IScopedCache<,>`, local lock and idempotency store) and the
distributed set (`IRedisCache`, `IDistributedCache`, `IConnectionMultiplexer`, Redis lock and idempotency store, and the
`AddSolRedisHealthCheck` factory), because both sets register the same lock and idempotency interfaces. No registration
was broken, so no test is `[Ignore]`d.

### Cache.Tests uses the Web SDK

**Original plan:** no project change.

**Actual implementation:** the Cache package brings only the DI and logging abstractions, so the test project had no
`ServiceProvider` or `AddLogging`. It now uses `Microsoft.NET.Sdk.Web`, like the HTTP, SQL, Api, and Auth test projects,
instead of adding packages.

