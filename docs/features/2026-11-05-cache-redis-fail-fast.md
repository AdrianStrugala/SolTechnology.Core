---
status: completed
created: 2026-11-05
completed: 2026-11-05
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

- [x] With `IsConnected = false`, `GetOrAdd` returns the factory result without touching `IDistributedCache`;
  `TryAcquireLock` returns `null`; `TryAdd` returns `true`; `Get` returns `null`; `SetResponse` and `Remove` return;
  none of them calls `GetDatabase`.
- [x] The skip logs `Redis not connected; skipping [{Operation}] for [{Key}]` at Warning at most once per operation in
  30 seconds, and at Debug otherwise.
- [x] A container test stops Redis and `GetOrAdd` returns the factory value in under 1 second, the lock returns `null`,
  and `TryAdd` returns `true`; after Redis starts again caching works within 20 seconds.
- [x] The container test failed before the change (the stopped-Redis call took longer than 1 second).
- [x] `AddSolDistributedCache_RedisDownAtStart_FailsOpen` stays green (the health check still reports `Unhealthy`).
- [x] `git diff` touches no `ModuleInstaller.cs` and no public symbol.
- [x] `dotnet build SolTechnology.Core.slnx` and `dotnet test SolTechnology.Core.slnx` are green; the container test
  passes three runs in a row. (Exception: one SQL DACPAC test failed for an environmental reason unrelated to Cache;
  see Completion summary.)

## Completion summary

Delivered on 2026-11-05 in one session, as planned.

- **Gate.** The new internal `RedisConnectionGate` (`src/SolTechnology.Core.Cache/RedisConnectionGate.cs`) returns
  `IsClosed = true` when `IConnectionMultiplexer.IsConnected` is `false` and logs
  `Redis not connected; skipping [{Operation}] for [{Key}]` in the consumer's category. Log volume: Warning for the first
  skip of an operation, then Debug for the same operation until 30 seconds have passed, per consumer instance (a
  lock-free `ConcurrentDictionary` of last-warning times on `TimeProvider`). This keeps one Warning per operation every
  30 seconds during an outage while the Debug entries still show every skipped key.
- **Consumers.** `RedisCache.GetOrAdd` checks before the read (skip: return the factory result, no write) and again
  before the write (the connection may drop during the read or the factory). `TryAcquireLock` checks after the expiry
  validation and the cancellation check, so a bad expiry still throws, and returns `null`. The idempotency store checks
  in all four operations (`TryAdd` → `true`, `Get` → `null`, `SetResponse` and `Remove` return). `RedisCache` got the
  shared `IConnectionMultiplexer` as a constructor dependency; it is internal and DI-resolved, so `ModuleInstaller.cs`,
  the DI registrations, and the public API are unchanged. The health check is unchanged and still reports `Unhealthy`.
- **Tests.** Unit tests with a substituted multiplexer (`IsConnected = false`) for the lock, the idempotency store, and
  `RedisCache` assert the fail-open results, no `GetDatabase` or `IDistributedCache` call, and the new log entry;
  `RedisConnectionGateTests` pins the Warning/Debug window with `FakeTimeProvider`. The container test
  `Redis/RedisOutageTests` runs its own `redis:7-alpine` on a free fixed host port (three attempts if the port is
  taken), stops it, and asserts that `GetOrAdd`, `TryAcquireLock`, and `TryAdd` together finish in under 1 second with
  fail-open results; after the start it polls (100 ms interval, 20 seconds budget) until a second `GetOrAdd` is served
  from Redis. Existing unit tests that substitute `IConnectionMultiplexer` now set `IsConnected = true`, because a
  substitute reports `false` by default.
- **Fail-before.** Against `574d800b` the lock and idempotency unit tests failed (`GetDatabase` was called; `TryAdd`
  returned `false` from the substitute database) and the container test failed with the three outage calls taking
  23.7 seconds instead of under 1 second. The `RedisCache` unit tests need the new constructor, so the container test is
  their fail-before evidence.
- **Verification.** The container test passed three runs in a row (3–5 seconds each). `dotnet build
  SolTechnology.Core.slnx` reported 0 errors. `dotnet test SolTechnology.Core.slnx`: 17 of 18 projects green
  (`SolTechnology.Core.Cache.Tests` 56/56; `DreamTravel.Component.Tests` 31/31 when run alone, because in the shared
  run it competed with the SQL tests for one Docker SQL Server's memory). `SolTechnology.Core.SQL.Tests` had 14/15:
  `DeployDacpac_DataLossAllowedByOptions_UpgradesInPlaceAndLogsBracketedTemplates` failed in every run with SQL Server
  dropping the connection (error 8645, waiting for memory) while a parallel SQL session changed and tested that module;
  the SQL tests do not reference the Cache package.
- **Docs.** `docs/Cache.md` (features, a `GetOrAdd` Redis-state table, lock and idempotency rows),
  `docs/architecture/cache.md` (fail fast while disconnected, reconnection and why there is no custom reconnect,
  logging), the release-notes input in
  [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md), and the delivered follow-up in
  [`2026-11-02-core-2-0-cache`](2026-11-02-core-2-0-cache.md).

## Deviations

None of substance. The feature brief was written before the code and committed with the abandonment of
`2026-11-04-cache-redis-reconnect`, so the links from that record and from the release feature resolve at every
commit. `docs/Cache.md` grew to 306 lines (Public Documentation Guide rule 7: assess at 300); the module does not need a
split, the extra lines are the `GetOrAdd` Redis-state table.

## Follow-ups

- During an outage the per-call failure warnings of the consumers (`Distributed cache GET failed …`,
  `Lock acquisition failed …`, `Idempotency … failed …`) are replaced by the gate's entry; release 05 states this in the
  2.0 release notes (input added to the umbrella brief). Whether it needs an `L-nn` row is the user's call.
- Releasing a Redis lock handle while disconnected still waits for the timeout (out of scope; best-effort, the key
  expires via TTL).
