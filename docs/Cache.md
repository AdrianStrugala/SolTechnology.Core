## SolTechnology.Core.Cache

Caching of async results behind one `GetOrAdd<TKey, TItem>` contract, in memory (`ISingletonCache`,
`IScopedCache<TKey, TItem>`) or in Redis (`IRedisCache`), plus a distributed lock and an idempotency store on
the same Redis.

### Features

- **One contract, three lifetimes** — process-wide in memory, per DI scope, or shared across instances in Redis.
- **Single flight** — concurrent misses on one key run the factory once; every caller awaits the same result.
- **Failures are not cached** — a faulted or cancelled factory result is evicted, so the next call retries.
- **Type-namespaced keys** — the same key value used with different `TKey`/`TItem` types never shares an entry,
  and keys carry no assembly versions, so a redeploy keeps the cache warm.
- **Fail-open, fail-fast Redis** — a Redis error logs a warning and the factory result is returned uncached; while the
  connection is down, the cache, the lock, and the idempotency store skip Redis at once instead of waiting for a
  timeout. Redis being down when the host starts does not stop it: the shared connection is opened with
  `abortConnect=false` (overriding the connection string) and reconnects by itself in the background.
- **Distributed lock** — `TryAcquireLock` with expiry and fencing, in-process or Redis `SET NX`.
- **Idempotency store** — reserve, replay, and release request keys, in-process or in Redis.
- **Fail-fast configuration** — an empty `InstanceName` or `ConnectionString`, or `ExpirationSeconds <= 0`, stops the
  host at start.
- **Health check** — `AddSolRedisHealthCheck()` pings Redis through the shared connection.

### Registration

Register the in-memory tier with one call (DreamTravel's API host):

```csharp
builder.Services.AddSolLocalCache();
```

Register Redis, then the lock and the idempotency store that reuse its connection:

```csharp
builder.Services.AddSolDistributedCache(
    builder.Configuration.GetRequiredSection("Redis").Get<DistributedCacheConfiguration>()!);
builder.Services.AddSolDistributedLock();
builder.Services.AddSolDistributedIdempotency(ttl: TimeSpan.FromHours(48));
builder.Services.AddHealthChecks().AddSolRedisHealthCheck(tags: ["ready"]);
```

Without Redis (local development, one instance), use the in-process variants with the same contracts:

```csharp
builder.Services.AddSolLocalLock();
builder.Services.AddSolLocalIdempotency();
```

### Configuration

| `CacheConfiguration` | Default | Purpose |
|---|---|---|
| `ExpirationMode` | `Absolute` | `Absolute` (from when the entry was added) or `Sliding` (from its last read). |
| `ExpirationSeconds` | `1200` | Entry lifetime; must be greater than 0. The default applies only when `AddSolLocalCache()` gets no configuration. |

| `DistributedCacheConfiguration` | Default | Purpose |
|---|---|---|
| `ConnectionString` | — | StackExchange.Redis connection string; must not be empty. `abortConnect` is always `false`. |
| `InstanceName` | — | Prefix of every Redis key the package writes; must not be empty. |
| `ExpirationSeconds` | `300` | Absolute lifetime of an `IRedisCache` entry; must be greater than 0. |

Bind both from configuration (DreamTravel's Worker binds `Cache`):

```json
{
  "Cache": { "ExpirationMode": "Absolute", "ExpirationSeconds": 300 },
  "Redis": { "ConnectionString": "localhost:6379", "InstanceName": "DreamTravel:", "ExpirationSeconds": 300 }
}
```

```csharp
var cacheConfiguration = builder.Configuration.GetSection("Cache").Get<CacheConfiguration>();
builder.Services.AddSolLocalCache(cacheConfiguration);
```

`InstanceName` prefixes cache, lock, and idempotency keys:

| Store | Redis key |
|---|---|
| `IRedisCache` | `{InstanceName}{TKey type}:{TItem type}:{JSON key}` |
| Redis lock | `{InstanceName}lock:{name}` |
| Redis idempotency store | `{InstanceName}idempotency:{key}` |

Upgrading from 1.x: `InstanceName` had the default `"SolTechnology:"`. Set `InstanceName = "SolTechnology:"` to
keep the 1.x lock and idempotency keys, so 1.x and 2.x instances still see each other's locks and idempotency records
during a rolling deploy. Cache entries start cold either way, because 2.0 adds the type names to cache keys.

### Usage

#### In-memory caches in a decorator

DreamTravel caches its Google client with both in-memory tiers and registers the decorator with Scrutor:

```csharp
public class GoogleHTTPClientCachingDecorator(
    IGoogleHTTPClient innerClient,
    IScopedCache<string, City> scopedCache,
    ISingletonCache singletonCache) : IGoogleHTTPClient
{
    // Same city name several times in one request → one HTTP call.
    public Task<City> GetLocationOfCity(string cityName)
        => scopedCache.GetOrAdd(cityName, key => innerClient.GetLocationOfCity(key));

    // Reverse geocoding rarely changes → shared by every request until it expires.
    public Task<City> GetNameOfCity(City city)
        => singletonCache.GetOrAdd(city, key => innerClient.GetNameOfCity(key));

    // ...
}
```

```csharp
services.AddSolHTTPClient<IGoogleHTTPClient, GoogleHTTPClient, GoogleHTTPOptions>("Google");
services.Decorate(typeof(IGoogleHTTPClient), typeof(GoogleHTTPClientCachingDecorator));
```

| Cache | Lifetime | Key |
|---|---|---|
| `ISingletonCache` | Process, until expiry | JSON of the key, namespaced by `TKey` and `TItem` |
| `IScopedCache<TKey, TItem>` | One DI scope, for example one HTTP request | JSON of the key |
| `IRedisCache` | Shared by every instance, until expiry | See the key table in Configuration |

#### Redis cache

`IRedisCache` has the same contract and stores the item as JSON:

```csharp
public class CityLookup(IRedisCache cache, IGoogleHTTPClient client)
{
    public Task<City> Find(string cityName)
        => cache.GetOrAdd(cityName, key => client.GetLocationOfCity(key));
}
```

| Redis | `GetOrAdd` |
|---|---|
| Connected | Returns the cached item, or runs the factory and caches its result. |
| Command fails | Logs a warning with the exception and returns the factory result uncached. |
| Connection down | Runs the factory at once without touching Redis and logs `Redis not connected; skipping [{Operation}] for [{Key}]`: Warning for the first skip of an operation in 30 seconds, Debug for the rest. The lock and the idempotency store log the same entry. |
| Back up | StackExchange.Redis reconnects the same connection by itself; caching resumes without a restart. |

#### Distributed lock

`TryAcquireLock` returns a handle, or `null` when another holder has the lock:

```csharp
public class SettlementPoller(IDistributedLockService locks)
{
    public async Task Poll(CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireLock("settlement/batch-process", TimeSpan.FromMinutes(5), ct);
        if (handle is null)
        {
            return;
        }

        // ... process the batch; one instance at a time
    }
}
```

| Case | Behaviour |
|---|---|
| Acquired | Returns an `IAsyncDisposable`; disposing it releases the lock. Disposing twice is harmless. |
| Held by another caller | Returns `null` at once; never waits. |
| Expiry passed | The lock is free again: Redis deletes the key, and the in-process lock lets the next `TryAcquireLock` take it over. Disposing the expired handle does not release the new holder (fencing); the Redis lock logs the warning `Lock [{LockKey}] expired before release`, a sign that the expiry is shorter than the work. |
| Backend failure | Returns `null` and logs a warning; never throws. While the Redis connection is down it returns `null` at once. |
| Expiry zero, negative, or over 365 days | Throws `ArgumentOutOfRangeException` before the lock is touched; both implementations apply the same rule. |
| Caller cancellation | Throws `OperationCanceledException`. |

#### Idempotency store

`IIdempotencyStore` lets a retried request with the same `Idempotency-Key` replay the first response:

| Operation | Behaviour |
|---|---|
| `TryAdd(key)` | Reserves the key atomically; `false` when another request holds it. |
| `Get(key)` | The stored response, or `null` while the first request is still running. |
| `SetResponse(key, response)` | Stores status, headers, and body for replay until the TTL (default 24 hours) passes. |
| `Remove(key)` | Releases the key so the client can retry, for example after an exception. |

The Redis store fails open: when Redis is down, `TryAdd` returns `true` at once, so a retried request runs again. Pair
it with a downstream idempotency key when a duplicate side effect is unacceptable. A middleware that uses the store:

```csharp
app.Use(async (context, next) =>
{
    var store = context.RequestServices.GetRequiredService<IIdempotencyStore>();
    var key = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
    if (string.IsNullOrEmpty(key))
    {
        await next();
        return;
    }

    var stored = await store.Get(key, context.RequestAborted);
    if (stored is not null)
    {
        context.Response.StatusCode = stored.StatusCode;
        foreach (var header in stored.Headers)
        {
            context.Response.Headers[header.Key] = header.Value;
        }
        await context.Response.Body.WriteAsync(stored.Body, context.RequestAborted);
        return;
    }

    if (!await store.TryAdd(key, context.RequestAborted))
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        return;
    }

    var originalBody = context.Response.Body;
    using var buffer = new MemoryStream();
    context.Response.Body = buffer;
    try
    {
        await next();
        if (context.Response.StatusCode is >= 200 and < 300)
        {
            await store.SetResponse(key, new StoredResponse
            {
                StatusCode = context.Response.StatusCode,
                Headers = context.Response.Headers.ToDictionary(h => h.Key, string[] (h) => h.Value.ToArray()!),
                Body = buffer.ToArray()
            }, context.RequestAborted);
        }
        else
        {
            await store.Remove(key, context.RequestAborted);
        }

        buffer.Seek(0, SeekOrigin.Begin);
        await buffer.CopyToAsync(originalBody, context.RequestAborted);
    }
    catch
    {
        await store.Remove(key);
        throw;
    }
    finally
    {
        context.Response.Body = originalBody;
    }
});
```

#### Cache, lock, or idempotency

| You need… | Use |
|---|---|
| Don't **compute** this twice | Cache (`GetOrAdd`) |
| Don't **do** this twice at the same time | Lock (`TryAcquireLock`) |
| Don't **do** this twice, even across retries minutes apart | Idempotency store |

### Testing

The in-memory tier needs no fixture. For Redis, [`SolTechnology.Core.Redis.Testing`](Redis.Testing.md) boots a
container:

```csharp
[Test]
public async Task GetOrAdd_SecondCall_ReturnsCachedItem()
{
    // Arrange
    await using var redis = new RedisFixture();
    await redis.InitializeAsync();
    var services = new ServiceCollection().AddLogging();
    services.AddSolDistributedCache(new DistributedCacheConfiguration
    {
        ConnectionString = redis.ConnectionString,
        InstanceName = "tests:"
    });
    await using var provider = services.BuildServiceProvider();
    var cache = provider.GetRequiredService<IRedisCache>();
    await cache.GetOrAdd("wroclaw", _ => Task.FromResult(640_000));

    // Act
    var population = await cache.GetOrAdd("wroclaw", _ => Task.FromResult(0));

    // Assert
    population.Should().Be(640_000);
}
```

### Conventions

- Give every application its own `InstanceName` when apps share one Redis; keep `"SolTechnology:"` only to stay
  compatible with 1.x locks and idempotency records.
- Use the cache for idempotent reads only; the factory may run again after an expiry or a failure.
- Include the tenant or principal in lock names, for example `$"settlements/{tenantId}/batch"`.
- Set a lock expiry 2–5 times the expected work duration.
- Store only 2xx responses in the idempotency store; call `Remove` on failures so clients can retry.

### What ships in DI

| Call | Registers | Lifetime |
|---|---|---|
| `AddSolLocalCache(CacheConfiguration?)` | `ISingletonCache`, `IScopedCache<,>`, `IMemoryCache`, validated `CacheConfiguration` | Singleton, scoped, singleton |
| `AddSolDistributedCache(DistributedCacheConfiguration)` | `IRedisCache`, `IDistributedCache`, one `IConnectionMultiplexer`, validated `DistributedCacheConfiguration` | Singleton |
| `AddSolLocalLock()` | In-process `IDistributedLockService` (uses a registered `TimeProvider` when present) | Singleton |
| `AddSolDistributedLock()` | Redis `IDistributedLockService`; requires `AddSolDistributedCache` | Singleton |
| `AddSolLocalIdempotency(TimeSpan?)` | In-process `IIdempotencyStore` | Singleton |
| `AddSolDistributedIdempotency(TimeSpan?)` | Redis `IIdempotencyStore`; requires `AddSolDistributedCache` | Singleton |
| `AddSolRedisHealthCheck(...)` on `IHealthChecksBuilder` | Redis `PING` check, default name `redis` | — |
