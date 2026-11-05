---
spec: 2026-11-04-cache-redis-reconnect
step: 01
status: to-do
---

# Step 01: Route every Redis consumer through an internal connection provider

## Summary
Introduce the internal `RedisConnectionProvider`, which creates and owns the one Redis multiplexer, and make the
Redis lock (including its release handle), the idempotency store, and the health check take `IDatabase` from it on
every call. No reconnect yet and no behaviour change: this step only removes every long-lived reference to the
multiplexer, so step 03 can swap it safely.

## Affected components
- `src/SolTechnology.Core.Cache/RedisMultiplexerFactory.cs` — NEW — `internal interface IRedisMultiplexerFactory` with `IConnectionMultiplexer Create(ConfigurationOptions options)`; `internal sealed class RedisMultiplexerFactory : IRedisMultiplexerFactory` returning `ConnectionMultiplexer.Connect(options)`
- `src/SolTechnology.Core.Cache/RedisConnectionProvider.cs` — NEW — `internal sealed class RedisConnectionProvider(IOptions<DistributedCacheConfiguration> options, IRedisMultiplexerFactory factory) : IDisposable, IAsyncDisposable`
- `src/SolTechnology.Core.Cache/ModuleInstaller.cs` — EDIT — registrations below; the file stays ≤ 150 lines
- `src/SolTechnology.Core.Cache/RedisDistributedLockService.cs` — EDIT — constructor takes `RedisConnectionProvider connection`; `LockHandle` takes the provider instead of an `IDatabase`
- `src/SolTechnology.Core.Cache/RedisIdempotencyStore.cs` — EDIT — constructor takes `RedisConnectionProvider connection`
- `src/SolTechnology.Core.Cache/HealthChecks/RedisHealthCheck.cs` — EDIT — constructor takes `RedisConnectionProvider connection`; class XML doc no longer names `IConnectionMultiplexer`
- `src/SolTechnology.Core.Cache/HealthChecks/RedisHealthCheckExtensions.cs` — EDIT — factory resolves `RedisConnectionProvider`; XML doc says "requires `AddSolDistributedCache(...)`" instead of "requires `IConnectionMultiplexer`"
- `tests/SolTechnology.Core.Cache.Tests/RedisConnectionProviderTests.cs` — NEW — unit tests below
- `tests/SolTechnology.Core.Cache.Tests/FakeMultiplexerFactory.cs` — NEW (premortem #9) — `internal sealed class FakeMultiplexerFactory(Func<ConfigurationOptions, IConnectionMultiplexer> onCreate) : IRedisMultiplexerFactory`: `Create` increments a thread-safe `CreateCalls` counter, records the received `ConfigurationOptions` in a `Received` list, and returns `onCreate(options)`; the delegate lets steps 03 and 04 block on a gate, throw, or return a sequence. NSubstitute cannot proxy the internal `IRedisMultiplexerFactory` without `InternalsVisibleTo("DynamicProxyGenAssembly2")` in the shipped csproj, which this plan does not add
- `tests/SolTechnology.Core.Cache.Tests/TestRedisConnection.cs` — NEW — `internal static class TestRedisConnection` with `RedisConnectionProvider For(IConnectionMultiplexer multiplexer, string instanceName = "test:")`: options with `ConnectionString = "localhost:6379"`, a `FakeMultiplexerFactory` returning `multiplexer`; step 03 extends it with `TimeProvider` and logger parameters
- `tests/SolTechnology.Core.Cache.Tests/RedisDistributedLockServiceTests.cs` — EDIT — build the SUT on `TestRedisConnection.For(redis)`; `TryAcquireLock_LogsBracketedLockKey` re-sequences its `GetDatabase` returns, because the release now calls `GetDatabase()` too (sequence: acquire `db`, release `db`, acquire-held `db`, acquire-failing throws, acquire `db`, release `db` whose script throws)
- `tests/SolTechnology.Core.Cache.Tests/RedisIdempotencyStoreTests.cs` — EDIT — `CreateSut` takes the multiplexer substitute and wraps it with `TestRedisConnection.For`
- `tests/SolTechnology.Core.Cache.Tests/Redis/RedisLockReleaseTests.cs` — EDIT — the SUT uses `TestRedisConnection.For(RedisTestEnvironment.RawConnection)`; the test never disposes that provider, because disposing it would dispose the shared `RawConnection`
- `tests/SolTechnology.Core.Cache.Tests/Redis/DistributedCacheRegistrationTests.cs` — EDIT — `AddSolDistributedCache_UsesSingleMultiplexer` compares the `ConnectionMultiplexerFactory` result with `RedisConnectionProvider.Multiplexer` (step 02 deletes the file)
- `tests/SolTechnology.Core.Cache.Tests/ModuleResolutionTests.cs` — EDIT — NEW test `AddSolDistributedCache_SyncDispose_DoesNotThrow`

## Changes
- `RedisConnectionProvider.Multiplexer`: created on first access, once, through `Lazy<IConnectionMultiplexer>` with `LazyThreadSafetyMode.ExecutionAndPublication`; options are `ConfigurationOptions.Parse(options.Value.ConnectionString)` with `AbortOnConnectFail = false` (moved from the installer lambda, `ModuleInstaller.cs:81-86`).
- `RedisConnectionProvider.GetDatabase()` returns `Multiplexer.GetDatabase()`.
- `RedisConnectionProvider.DisposeAsync()` and `Dispose()` dispose the multiplexer only if it was created; a second call of either does nothing. `Dispose()` exists because a container disposed synchronously throws `InvalidOperationException` for a resolved singleton that implements only `IAsyncDisposable` (today's `ConnectionMultiplexer` registration implements both).
- `ModuleInstaller.AddSolDistributedCache`: `services.TryAddSingleton<IRedisMultiplexerFactory, RedisMultiplexerFactory>()`; `services.AddSingleton<RedisConnectionProvider>()`; the `IConnectionMultiplexer` registration becomes `sp => sp.GetRequiredService<RedisConnectionProvider>().Multiplexer` (kept until step 03 deletes it, Q1 = A); `RedisCacheOptions.ConnectionMultiplexerFactory` returns `Task.FromResult(provider.GetRequiredService<RedisConnectionProvider>().Multiplexer)` (removed in step 02).
- `ModuleInstaller.AddSolDistributedIdempotency`: the lambda passes `sp.GetRequiredService<RedisConnectionProvider>()`.
- `RedisDistributedLockService.TryAcquireLock`: `connection.GetDatabase()` per call; returns `new LockHandle(connection, key, value, logger)`.
- `LockHandle.DisposeAsync`: calls `connection.GetDatabase()` inside the release's `try`, so a release after a swap uses the current multiplexer and a `GetDatabase()` failure takes the existing release-failed path. Release script, log templates, and the release-once guard are unchanged.
- `AddSolRedisHealthCheck()` now requires `AddSolDistributedCache` instead of any registered `IConnectionMultiplexer` (P-21).
- `RedisIdempotencyStore.TryAdd/Get/SetResponse/Remove` and `RedisHealthCheck.CheckHealthAsync`: `connection.GetDatabase()` per call; templates, return values, and fail-open behaviour are unchanged.
- No public symbol changes. `IRedisCache` still goes through `IDistributedCache` in this step.

## Acceptance criteria
- [ ] `RedisConnectionProviderTests.Multiplexer_ConcurrentFirstAccess_CreatesOnce` — 20 parallel reads of `Multiplexer` call `IRedisMultiplexerFactory.Create` exactly once (`FakeMultiplexerFactory.CreateCalls == 1`, the fake returning a substitute `IConnectionMultiplexer`).
- [ ] `RedisConnectionProviderTests.Multiplexer_ConnectionStringAbortConnectTrue_CreatesWithAbortOnConnectFailFalse` — connection string `localhost:6379,abortConnect=true`; the `ConfigurationOptions` passed to `Create` has `AbortOnConnectFail == false`.
- [ ] `RedisConnectionProviderTests.DisposeAsync_CreatedMultiplexer_DisposesItOnce`, `DisposeAsync_NeverAccessed_CreatesNothing`, and `Dispose_CreatedMultiplexer_DisposesItOnce` pass.
- [ ] `ModuleResolutionTests.AddSolDistributedCache_SyncDispose_DoesNotThrow` — resolve `IRedisCache` from `BuildServiceProvider(StrictValidation)` with the unreachable connection string, then `provider.Dispose()` does not throw.
- [ ] `grep -rln "IConnectionMultiplexer" src/SolTechnology.Core.Cache --include=*.cs` lists only `RedisConnectionProvider.cs`, `RedisMultiplexerFactory.cs`, and `ModuleInstaller.cs`.
- [ ] `RedisDistributedLockService.cs` has no constructor parameter or field of type `IDatabase` (`grep -n "IDatabase" src/SolTechnology.Core.Cache/RedisDistributedLockService.cs` shows only local variables).
- [ ] Existing tests stay green. Unchanged: `ModuleInstallerTests.AddSolDistributedCache_RedisDownAtStart_FailsOpen`, `ModuleResolutionTests.AddSolDistributedCache_ResolvesAllPublicServices_WithValidation`, `RedisKeysTests`, `RedisCacheTests`. Edited only in their SUT construction and `GetDatabase` sequencing (assertions and expected log lists unchanged): `RedisDistributedLockServiceTests`, `RedisIdempotencyStoreTests`, `RedisLockReleaseTests`; plus the edited `DistributedCacheRegistrationTests`.
- [ ] Premortem #9: `grep -rn "Substitute.For<IRedisMultiplexerFactory>" tests/SolTechnology.Core.Cache.Tests` and `grep -n "DynamicProxyGenAssembly2" src/SolTechnology.Core.Cache/SolTechnology.Core.Cache.csproj` both print nothing.
- [ ] `wc -l src/SolTechnology.Core.Cache/ModuleInstaller.cs` ≤ 150.
- [ ] `dotnet build SolTechnology.Core.slnx` reports 0 errors; `dotnet test tests/SolTechnology.Core.Cache.Tests` is green with Docker running.

## Open questions
- none

## Deviations
