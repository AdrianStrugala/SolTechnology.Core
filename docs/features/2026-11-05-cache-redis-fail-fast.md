---
status: in-progress
created: 2026-11-05
completed:
---

# Cache — fail fast while the Redis connection is down

> Historical delivery record. It may not describe the current system.

## Goal

While the shared Redis connection of `SolTechnology.Core.Cache` is down, `IRedisCache.GetOrAdd`, the Redis lock
(`TryAcquireLock`), and the Redis idempotency store (`TryAdd`, `Get`, `SetResponse`, `Remove`) skip Redis at once and
fail open with the results they already return on a Redis error, instead of waiting for the StackExchange.Redis
timeouts. When Redis comes back, StackExchange.Redis reconnects the same connection and the consumers use Redis again
without any action.

## Context

**Requested by:** the user on 2026-11-05 ("Porzuć duży plan. Zrób mały"), replacing the abandoned
[`2026-11-04-cache-redis-reconnect`](2026-11-04-cache-redis-reconnect.md). An experiment that day (table in that
record's Completion summary) showed that the shared multiplexer already recovers after Redis restarts, in 13–33 ms,
through both the `IDistributedCache` path and the direct `IDatabase` path. The observed problem was latency: while
Redis is down each call waits about 5–11 seconds (sync and async timeouts, two Redis operations in `GetOrAdd`) before
it fails open. That latency is the first follow-up of [`2026-11-02-core-2-0-cache`](2026-11-02-core-2-0-cache.md).

**Current state (`master@09974d7c`):**

| Symbol | File | Role |
|---|---|---|
| `RedisCache(IDistributedCache, IOptions<DistributedCacheConfiguration>, ILogger<RedisCache>)` | `src/SolTechnology.Core.Cache/RedisCache.cs` | Internal; resolved by DI through `AddSingleton<IRedisCache, RedisCache>()`, so a new constructor dependency needs no `ModuleInstaller` change |
| `RedisDistributedLockService(IConnectionMultiplexer, …)` | `src/SolTechnology.Core.Cache/RedisDistributedLockService.cs` | Internal; already holds the shared multiplexer |
| `RedisIdempotencyStore(IConnectionMultiplexer, …, TimeSpan)` | `src/SolTechnology.Core.Cache/RedisIdempotencyStore.cs` | Internal; already holds the shared multiplexer |
| `RedisHealthCheck` | `src/SolTechnology.Core.Cache/HealthChecks/RedisHealthCheck.cs` | Unchanged; it must keep reporting `Unhealthy` while Redis is down |

**Gates.** No public or protected symbol changes (all three consumers are `internal sealed`), no `ModuleInstaller.cs`
change, no `Directory.Build.props` change, no persisted contract change, and no new package. None of the `CLAUDE.md §4`
premortem triggers applies, and no `CLAUDE.md §2` confirmation is needed. Single-session change per
[delivery workflow](../architecture/delivery-workflow.md): no working folder.

## Scope

### In scope

- `src/SolTechnology.Core.Cache`: a new internal `RedisConnectionGate` that checks `IConnectionMultiplexer.IsConnected`
  and logs the skip; `RedisCache` takes the shared `IConnectionMultiplexer` as an extra constructor dependency;
  the three consumers ask the gate before each Redis operation.
- `tests/SolTechnology.Core.Cache.Tests`: unit tests with a substituted multiplexer, and a container test that stops and
  starts Redis on a fixed host port.
- `docs/Cache.md`, `docs/architecture/cache.md`, the release-notes input in
  [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md), and the fail-fast follow-up of
  [`2026-11-02-core-2-0-cache`](2026-11-02-core-2-0-cache.md).

### Out of scope

- A custom reconnect or connection swap (abandoned with `2026-11-04-cache-redis-reconnect`).
- The release of a lock handle (`DisposeAsync`) while disconnected; it keeps its best-effort release that waits for the
  timeout, then logs and lets the TTL expire the key.
- The health check, the DI registrations, and `DistributedCacheConfiguration`.
- A connection that is broken but still reports `IsConnected = true` (for example a network partition without a reset);
  such calls still wait for the timeout until StackExchange.Redis marks the connection failed.

## Implementation plan

1. Tests first: lock and idempotency unit tests with `IsConnected = false` that assert no `GetDatabase` call, the
   fail-open result, and the new log; a container test with a fixed host port (start → cache works; stop → fail-open in
   under 1 second; start → caching works again within 20 seconds). Run them against the current code and record the
   failures.
2. Add `RedisConnectionGate` (internal): `IsClosed(operation, key)` returns `false` when the multiplexer is connected;
   otherwise it logs `Redis not connected; skipping [{Operation}] for [{Key}]` and returns `true`.
3. Use the gate in `RedisCache.GetOrAdd` (before the read and before the write), `RedisDistributedLockService.TryAcquireLock`
   (after argument validation, so a bad expiry still throws), and the four idempotency operations.
4. Log volume: the first skip of an operation logs a Warning; further skips of the same operation in the next 30
   seconds log at Debug, per consumer instance. Timing uses `TimeProvider` so a unit test pins the window.
5. Update existing unit tests that substitute `IConnectionMultiplexer` to report `IsConnected = true`.
6. Docs: module page, architecture page, release-notes input, cache follow-up.

## Acceptance criteria

- [ ] With `IsConnected = false`, `GetOrAdd` returns the factory result without touching `IDistributedCache`;
  `TryAcquireLock` returns `null`; `TryAdd` returns `true`; `Get` returns `null`; `SetResponse` and `Remove` return;
  none of them calls `GetDatabase`.
- [ ] The skip logs `Redis not connected; skipping [{Operation}] for [{Key}]` at Warning at most once per operation in
  30 seconds, and at Debug otherwise.
- [ ] A container test stops Redis and `GetOrAdd` returns the factory value in under 1 second, the lock returns `null`,
  and `TryAdd` returns `true`; after Redis starts again caching works within 20 seconds.
- [ ] The container test failed before the change (the stopped-Redis call took longer than 1 second).
- [ ] `AddSolDistributedCache_RedisDownAtStart_FailsOpen` stays green (the health check still reports `Unhealthy`).
- [ ] `git diff` touches no `ModuleInstaller.cs` and no public symbol.
- [ ] `dotnet build SolTechnology.Core.slnx` and `dotnet test SolTechnology.Core.slnx` are green; the container test
  passes three runs in a row.

## Completion summary

## Deviations

## Follow-ups
