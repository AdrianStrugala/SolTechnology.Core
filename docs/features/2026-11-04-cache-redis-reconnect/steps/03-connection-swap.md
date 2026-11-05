---
spec: 2026-11-04-cache-redis-reconnect
step: 03
status: to-do
---

# Step 03: Atomic connection swap with delayed disposal

## Summary
Give `RedisConnectionProvider` an internal `Reconnect()` that builds a new multiplexer, swaps it in atomically, and
disposes the old one after a grace period, with one reconnect at a time and timing through `TimeProvider`. Nothing in
production calls it yet; step 04 adds the trigger. This step also removes the `IConnectionMultiplexer` registration
from `AddSolDistributedCache` (Q1 = A, user, 2026-11-04; breaking change C-31, P-21), because from here on a
multiplexer handed out once can later be disposed.

## Affected components
- `src/SolTechnology.Core.Cache/RedisConnectionProvider.cs` — EDIT — constructor gains `TimeProvider timeProvider` and `ILogger<RedisConnectionProvider> logger`; `Task Reconnect()`; tracked, cancellable grace disposal of old multiplexers
- `src/SolTechnology.Core.Cache/RedisConnectionLogMessages.cs` — NEW — `internal static partial class RedisConnectionLogMessages` with `[LoggerMessage]` methods for EventIds `2302`, `2303`, `2304` (`2301` is added in step 04)
- `src/SolTechnology.Core.Cache/ModuleInstaller.cs` — EDIT — provider registered through a factory lambda passing `sp.GetService<TimeProvider>() ?? TimeProvider.System` (the `AddSolLocalLock` pattern); the `services.AddSingleton<IConnectionMultiplexer>(…)` registration added in step 01 is deleted (Q1 = A); the XML docs of `AddSolDistributedCache`, `AddSolDistributedLock`, and `AddSolDistributedIdempotency` stop naming `IConnectionMultiplexer` ("uses the Redis connection of `AddSolDistributedCache`"), moved here from step 05 so the DI docs change in the same PR as the registration
- `tests/SolTechnology.Core.Cache.Tests/CapturingLogger.cs` — EDIT — `CapturedLog` gains `EventId EventId` and `Exception? Exception` (appended positional members; no test constructs `CapturedLog` directly); NEW `IReadOnlyList<CapturedLog> Snapshot()` returning a copy under the existing lock, for assertions while background work may still log
- `tests/SolTechnology.Core.Cache.Tests/TestRedisConnection.cs` — EDIT — `For(...)` gains optional `TimeProvider? timeProvider` and `ILogger<RedisConnectionProvider>? logger` parameters
- `tests/SolTechnology.Core.Cache.Tests/RedisConnectionSwapTests.cs` — NEW — provider swap tests (NSubstitute multiplexers, `FakeTimeProvider`)
- `tests/SolTechnology.Core.Cache.Tests/RedisConsumersAfterSwapTests.cs` — NEW — every consumer uses the new multiplexer after a swap
- `tests/SolTechnology.Core.Cache.Tests/ModuleResolutionTests.cs` — EDIT — `AddSolDistributedCache_ResolvesAllPublicServices_WithValidation` stops resolving `IConnectionMultiplexer` and asserts `provider.GetService<IConnectionMultiplexer>()` is `null`; NEW test `AddSolDistributedCache_ConsumerInjectsConnectionMultiplexer_FailsValidationOnBuild`

## Changes
- Current multiplexer kept in a field read with `Volatile.Read` and replaced with `Interlocked.Exchange`; the step 01 `Lazy` is replaced by a create-once-under-lock first access, so the initial creation and `Reconnect()` share one code path. A first access whose `factory.Create` throws publishes nothing and lets the next access retry (unlike `Lazy` with `ExecutionAndPublication`, which caches the exception).
- `Reconnect()`: single flight through `Interlocked.CompareExchange(ref _reconnecting, 1, 0)`; a call while one runs returns the running task. The winner calls `RedisReconnectPolicy.Reconnected()` in step 04 (the hook point is the gate, so direct calls and `ReportError` calls behave the same). The new multiplexer is created on the thread pool (`Task.Run(() => factory.Create(options))`) from freshly parsed options with `AbortOnConnectFail = false`.
- `Reconnect()` completes right after the swap and the `2302` log; it never awaits the grace period. The flag is reset in a `finally`.
- Grace disposal: before `Reconnect()` returns, the provider starts a tracked background task for the old multiplexer that calls `Task.Delay(gracePeriod, timeProvider, disposalToken)` synchronously (so the `FakeTimeProvider` timer exists when `Reconnect()` returns), then `DisposeAsync()` on the old multiplexer; a failure logs `2304` Warning `Disposing the previous Redis connection failed` with the exception. `disposalToken` is cancelled by the provider's disposal. Pending old multiplexers are kept in a list guarded by a lock; each is removed from the list once its grace disposal finishes, whether it succeeded or logged `2304` (premortem #7).
- `2302` Information `Redis connection replaced; the previous connection is disposed in [{GracePeriodMs}] ms` is logged after the swap.
- Grace period: the parsed `ConfigurationOptions.AsyncTimeout` plus `ConfigurationOptions.HeartbeatInterval`, in milliseconds (defaults 5000 ms + 1000 ms). StackExchange.Redis 3.0.0 times out backlogged commands only on its heartbeat (`PhysicalBridge.OnHeartbeat` → `StartBacklogProcessor`), so a command queued just before the swap can outlive `AsyncTimeout` by up to one heartbeat, and disposing earlier abandons it with `ObjectDisposedException` (`PhysicalBridge.Dispose`) (premortem #2). The old multiplexer's own `DisposeAsync` then sends `QUIT` and waits up to `AsyncTimeout` more. Computed once from the validated options; internal, not an option.
- `factory.Create` throws: log `2303` Error `Redis reconnect failed; keeping the current connection` with the exception; the current multiplexer stays; `Reconnect()` completes without throwing.
- `DisposeAsync()` and `Dispose()`: mark the provider disposed, cancel `disposalToken`, dispose the current multiplexer and every old one still waiting for its grace period immediately; `Reconnect()` after disposal does nothing; a multiplexer created by a `Reconnect()` that finishes after disposal is disposed at once and never published. `Multiplexer` and `GetDatabase()` after disposal throw `ObjectDisposedException` and never call `factory.Create`, so a first access during shutdown cannot create a connection nobody disposes (premortem #7); consumers already catch it and fail open, and the step 04 policy does not count it.
- Tests build the provider on `FakeMultiplexerFactory` (step 01, premortem #9); "`Create` blocks on a gate" and "`Create` throws once" are delegates passed to the fake.
- No template contains the connection string or other `ConfigurationOptions` values.
- Q1 = A applied: `ModuleInstaller.AddSolDistributedCache` registers no `IConnectionMultiplexer`; every package consumer already resolves `RedisConnectionProvider` (step 01), so no `src/` code resolves `IConnectionMultiplexer` from DI.

## Acceptance criteria
- [ ] `RedisConnectionSwapTests`:
  - `Reconnect_SwapsMultiplexer` — `Multiplexer` and `GetDatabase()` return the new instance after `await Reconnect()`.
  - `Reconnect_DisposesOldAfterGracePeriod` — connection string with `asyncTimeout=2000` and the default `HeartbeatInterval` (1 s), so the grace period is 3000 ms (premortem #2); after `await Reconnect()` returns (without advancing time), the old multiplexer is not disposed after advancing `FakeTimeProvider` by 2999 ms and is disposed once after 3000 ms; the `2302` entry carries `GracePeriodMs == 3000`. Disposal is observed by awaiting a `TaskCompletionSource` completed from the old substitute's `DisposeAsync()`, bounded with `WaitAsync(TimeSpan.FromSeconds(5))`; the "not disposed" check reads the same source's `Task.IsCompleted`. No real-time sleeps.
  - `Reconnect_ConcurrentCalls_CreateOneMultiplexer` — 20 parallel `Reconnect()` calls while `Create` blocks on a gate: `Create` is called twice in total (initial + one reconnect).
  - `Reconnect_FactoryThrows_KeepsCurrentAndLogs2303` — `Multiplexer` is unchanged; one `CapturedLog` with `EventId.Id == 2303`, Level Error, non-null `Exception`.
  - `Reconnect_ConnectionStringAbortConnectTrue_NewMultiplexerHasAbortOnConnectFailFalse`.
  - `DisposeAsync_DuringGracePeriod_DisposesOldAndCurrent` — after `Reconnect()` and without advancing time, `DisposeAsync()` disposes the old and the current multiplexer once each; advancing time afterwards disposes nothing again.
  - `Dispose_DuringGracePeriod_DisposesOldAndCurrent` — the same through the synchronous `Dispose()`.
  - `Multiplexer_FirstCreateThrows_NextAccessRetries` — `Create` throws once, then returns a substitute; the second access of `Multiplexer` returns it.
  - `Reconnect_AfterDispose_CreatesNothing`.
  - `Multiplexer_AfterDispose_ThrowsAndCreatesNothing` (premortem #7) — a provider that was never accessed is disposed; `Multiplexer` and `GetDatabase()` throw `ObjectDisposedException` and `FakeMultiplexerFactory.CreateCalls == 0`.
  - `Reconnect_GraceDisposalDone_NoPendingMultiplexerLeft` (premortem #7) — after three `Reconnect()` calls, each followed by advancing time past its grace period, an internal `PendingDisposalCount` reads `0` and each old multiplexer was disposed exactly once.
  - `Reconnect_LogsContainNoConnectionString` — connection string `localhost:6379,password=secret-value`; one successful and one failing `Reconnect()`; no captured template, property value, or exception message contains `secret-value`.
- [ ] `RedisConsumersAfterSwapTests` (substitute multiplexers whose `GetDatabase()` return distinct substitute `IDatabase` instances): after `Reconnect()`, `IRedisCache.GetOrAdd`, `TryAcquireLock`, `IIdempotencyStore.TryAdd`, and `RedisHealthCheck.CheckHealthAsync` call the new `IDatabase` and never the old one; `LockHandle_AcquiredBeforeSwap_ReleasesThroughNewDatabase` — the release script runs on the new `IDatabase`; `GetOrAdd_SwapDuringFactory_WritesThroughNewDatabase` — `Reconnect()` runs inside the factory; `StringGetAsync` was called on the old `IDatabase` and `StringSetAsync` on the new one.
- [ ] `ModuleResolutionTests.AddSolDistributedCache_ResolvesAllPublicServices_WithValidation` passes with `provider.GetService<IConnectionMultiplexer>()` asserted `null`.
- [ ] `ModuleResolutionTests.AddSolDistributedCache_ConsumerInjectsConnectionMultiplexer_FailsValidationOnBuild` — a singleton whose constructor takes `IConnectionMultiplexer`, registered next to `AddSolDistributedCache`, makes `BuildServiceProvider(StrictValidation)` throw `AggregateException` whose message names `IConnectionMultiplexer`.
- [ ] `grep -rn "IConnectionMultiplexer" src/SolTechnology.Core.Cache/ModuleInstaller.cs` prints nothing.
- [ ] `find src/SolTechnology.Core.Cache -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `dotnet build SolTechnology.Core.slnx` reports 0 errors; `dotnet test tests/SolTechnology.Core.Cache.Tests` is green with Docker running.

## Open questions
- none. Resolved:
  - **Q1 — What does `AddSolDistributedCache` register as `IConnectionMultiplexer` once the connection can be replaced?** Answered by the user on 2026-11-04: **A** — stop registering `IConnectionMultiplexer`; apps that inject it get a DI validation error at start and register their own connection. Removing the public DI registration is a breaking change; the answer is the `CLAUDE.md §2` confirmation (C-31, P-21). Rejected: B (registration returning the current multiplexer; a singleton holder gets `ObjectDisposedException` after the first reconnect) and C (keep the initial multiplexer forever; leaks a connection the package gave up on).

## Deviations
