# Cache

`SolTechnology.Core.Cache` caches async results in memory or in Redis behind one
`GetOrAdd<TKey, TItem>` contract, and ships a distributed lock and an idempotency store on the same
Redis connection. Consumer-facing usage is in [`docs/Cache.md`](../Cache.md); registration lives in
[`ModuleInstaller.cs`](../../src/SolTechnology.Core.Cache/ModuleInstaller.cs).

## Keys

`ISingletonCache` and `IRedisCache` build their keys with the internal `CacheKey` helper:
`{TKey type}:{TItem type}:{JSON key}`. The type names are namespace and name, with generic arguments
and arrays expanded recursively and no assembly information. Two call sites that use the same key
value with different types never share an entry, so Redis never deserializes one item type into
another, and a redeploy with new assembly versions keeps the cache warm. `IScopedCache<TKey, TItem>`
keeps the plain JSON key, because one instance serves one `TKey`/`TItem` pair.

`DistributedCacheConfiguration.InstanceName` prefixes every Redis key the package writes:

| Store | Redis key |
|---|---|
| `IRedisCache` | `{InstanceName}{TKey type}:{TItem type}:{JSON key}` |
| Redis lock | `{InstanceName}lock:{name}` |
| Redis idempotency store | `{InstanceName}idempotency:{key}` |

`InstanceName` has no default, so applications that share a Redis do not share a keyspace by
accident. The lock and idempotency key composition is the 1.x one, so `InstanceName =
"SolTechnology:"` (the 1.x default) keeps 1.x and 2.x instances seeing each other's locks and
idempotency records during a rolling deploy.

## Configuration validation

`AddSolLocalCache` and `AddSolDistributedCache` register their options with `ValidateOnStart`:
`ExpirationSeconds` must be greater than 0, and `ConnectionString` and `InstanceName` must not be
empty. Bad configuration stops the host instead of failing on the first cache call. `AddSolLocalCache()`
without a configuration uses a 20-minute absolute expiry; a configuration object passed in is used as
is, without falling back to that default.

## In-memory caches

Both in-memory caches store a `Lazy<Task<TItem>>` created with
`LazyThreadSafetyMode.ExecutionAndPublication`, so concurrent misses on one key run the factory once
and every caller awaits the same task (single flight). `SingletonCache` creates the entry under a
lock, because `IMemoryCache` has no atomic get-or-create. A faulted or cancelled task is removed, but
only while it is still the current entry, so the next call runs the factory again and a newer entry
is never evicted by an older failure.

## Redis connection

`AddSolDistributedCache` registers one `IConnectionMultiplexer`, built from the validated
`DistributedCacheConfiguration` on first resolution. The framework `IDistributedCache` gets the same
instance through `RedisCacheOptions.ConnectionMultiplexerFactory`, and the Redis lock, the Redis
idempotency store, and `AddSolRedisHealthCheck` resolve it too, so a process holds one connection.

The multiplexer is opened with `AbortOnConnectFail = false`, whatever the connection string says.
StackExchange.Redis defaults to `abortConnect=true` for non-Azure endpoints, which would make every
consumer throw on resolution while Redis is down at start. With `false`, resolution succeeds, the
connection retries in the background, and each consumer applies its own failure policy:

| Consumer | Redis unavailable |
|---|---|
| `IRedisCache` | Logs a warning and returns the factory result uncached. |
| Redis lock | Logs a warning and returns `null` (not acquired). |
| Redis idempotency store | `TryAdd` logs a warning and returns `true` (fail-open); `Get` returns `null`; `SetResponse` and `Remove` log and return. |
| Health check | Reports the configured failure status (default `Unhealthy`). |

The idempotency store fails open because blocking every request during an outage is worse than
allowing a retried request to run twice; callers that cannot accept a duplicate side effect need a
downstream idempotency key.

### Fail fast while disconnected

A command issued while the multiplexer is disconnected waits in the StackExchange.Redis backlog
until the sync or async timeout (5 seconds by default), and `IRedisCache.GetOrAdd` issues two, so
without a check an outage adds 5–11 seconds to every call before it fails open. The internal
`RedisConnectionGate` removes that wait: `IRedisCache.GetOrAdd` (before the read and again before
the write), `TryAcquireLock` (after the expiry validation), and the four idempotency operations
check `IConnectionMultiplexer.IsConnected` first and, when it is `false`, return the same fail-open
result without sending a command. `RedisCache` takes the shared multiplexer as a constructor
dependency for this; it is internal and resolved by DI, so the registrations did not change. The
health check does not use the gate: it still sends `PING` and reports the failure status.

The gate relies on StackExchange.Redis to notice the outage. A closed or reset socket flips
`IsConnected` at once; a connection that breaks without a reset (for example a network partition)
keeps `IsConnected = true` until the heartbeat marks it failed, and calls in that window still wait
for the timeout. Releasing a lock handle is not gated; it is best-effort and the key expires anyway.

### Reconnection

The package does not replace or force-reconnect the multiplexer. StackExchange.Redis reconnects the
existing one by itself, and the gate lets traffic through again as soon as `IsConnected` is `true`.
An experiment on 2026-11-05 with StackExchange.Redis 3.0.0, Microsoft.Extensions.Caching.StackExchangeRedis
10.0.1, and this wiring (Redis 7 in Docker on a fixed port) showed both the `IDistributedCache` path
and the direct `IDatabase` path recovering by themselves 13–33 ms after `docker start` or
`docker restart`, after a start with Redis down and after a stop. A package-owned connection
provider with a force-reconnect policy would add a connection swap, two options, and the removal of
the `IConnectionMultiplexer` registration (a DI break) without fixing an observed failure. The
container test `RedisOutageTests` pins the stop, fail-fast, start, and recovery sequence.

## Locks

`IDistributedLockService.TryAcquireLock` never waits: it returns a handle or `null`. Both
implementations validate `expiry` with the same rule (greater than zero, at most 365 days) and throw
`ArgumentOutOfRangeException` before touching the lock; caller cancellation throws
`OperationCanceledException`. Every other failure logs a warning and returns `null`.

- **In process (`AddSolLocalLock`):** one holder object per name with a deadline from the registered
  `TimeProvider`. A name is claimed with `TryAdd`, an expired holder is taken over with `TryUpdate`,
  and a handle removes only its own holder (compared by reference), once. An expired handle therefore
  cannot release the next holder, and released names do not leak.
- **Redis (`AddSolDistributedLock`):** `SET NX` with the expiry and a random token per acquisition.
  Release runs a Lua compare-and-delete, so an expired handle cannot delete another holder's key.
  When the script deletes nothing, the lock had expired before release, and the handle logs the
  warning `Lock [{LockKey}] expired before release`, a signal that the expiry is shorter than the
  work. A handle releases once; disposing it again does nothing.

The 365-day ceiling gives both implementations one documented rule; without it, `TimeSpan.MaxValue`
would overflow the in-process deadline.

## Logging

Log templates bracket every placeholder: `[{CacheKey}]` in the Redis cache, `[{LockKey}]` in both
locks, and `[{IdempotencyKey}]` in the Redis idempotency store. Fail-open paths log at Warning with
the exception; lock acquisition and release log at Debug.

A call skipped by the connection gate logs `Redis not connected; skipping [{Operation}] for [{Key}]`
without an exception, in the consumer's log category. `Operation` is the method name (`GetOrAdd`,
`TryAcquireLock`, `TryAdd`, `Get`, `SetResponse`, `Remove`) and `Key` is the key that consumer
logs elsewhere (the cache key without `InstanceName`, the full lock key, the idempotency key). One
template for all consumers gives one query for "Redis was down". To keep an outage from flooding
the logs, the first skip of an operation logs at Warning and further skips of the same operation in
the next 30 seconds log at Debug, per consumer instance.
