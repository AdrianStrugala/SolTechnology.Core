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

Commands issued while disconnected wait in the StackExchange.Redis backlog until the configured
timeout, so a Redis outage adds that timeout to each call instead of failing it at once. The
idempotency store fails open because blocking every request during an outage is worse than allowing
a retried request to run twice; callers that cannot accept a duplicate side effect need a downstream
idempotency key.

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
