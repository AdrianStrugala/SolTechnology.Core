# SolTechnology.Core.Cache

## Overview

Two-tier caching library with a **unified interface** — both tiers expose the same `GetOrAdd<TKey, TItem>` contract:

| Tier | Backing store | Registration | Interface |
|------|--------------|--------------|-----------|
| **Local** | `IMemoryCache` (in-process) | `AddLocalCache()` | `ISingletonCache`, `IScopedCache<TKey,TItem>` |
| **Distributed** | Redis via `StackExchange.Redis` | `AddDistributedCache(config)` | `IRedisCache` |

Both tiers can be used independently or together. Distributed tier is **fail-open** — Redis failures log a warning and fall through to the factory instead of throwing.

---

## Installation

```
dotnet add package SolTechnology.Core.Cache
```

---

## Registration

### Local cache (in-memory)

DreamTravel's API host registers the local tier with one line (`DreamTravel.Api/Program.cs`):

```csharp
builder.Services.AddLocalCache();
```

The Worker host binds expiration from configuration (`DreamTravel.Worker/Program.cs`):

```csharp
var cacheConfiguration = builder.Configuration.GetSection("Cache").Get<CacheConfiguration>()!;
builder.Services.AddLocalCache(cacheConfiguration);
```

### Distributed cache (Redis)

```csharp
services.AddDistributedCache(new DistributedCacheConfiguration
{
    ConnectionString = "localhost:6379",
    InstanceName = "DreamTravel:",
    ExpirationSeconds = 300
});
```

### Both tiers together

```csharp
builder.Services.AddLocalCache();
builder.Services.AddDistributedCache(
    builder.Configuration.GetSection("Redis").Get<DistributedCacheConfiguration>()!);
```

> Use `ISingletonCache` for hot in-process data and `IRedisCache` for shared cross-instance data.

---

## Configuration

### CacheConfiguration (local)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ExpirationMode` | `ExpirationMode` | `Absolute` | `Absolute` or `Sliding` |
| `ExpirationSeconds` | `int` | `1200` (20 min) | Time before entry expires |

Can be provided via `appsettings.json` — DreamTravel's Worker binds the `Cache` section:

```json
{
  "Cache": {
    "ExpirationMode": "Absolute",
    "ExpirationSeconds": 300
  }
}
```

### DistributedCacheConfiguration (Redis)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string` | — (required) | Redis connection string |
| `InstanceName` | `string` | `"SolTechnology:"` | Key prefix in Redis |
| `ExpirationSeconds` | `int` | `300` (5 min) | Default absolute TTL |

---

## Usage

All caches share the same contract: `GetOrAdd<TKey, TItem>(key, factory)`.  
Key is serialized to JSON internally — pass any object.

### Real example — a caching decorator over an HTTP client

DreamTravel wraps its Google geolocation client with a thin caching decorator
(`GoogleHTTPClientCachingDecorator`). It uses **both** local tiers — each one fits a
different access pattern:

```csharp
public class GoogleHTTPClientCachingDecorator(
    IGoogleHTTPClient innerClient,
    IScopedCache<string, City> scopedCache,
    ISingletonCache singletonCache) : IGoogleHTTPClient
{
    // Forward-geocode the same city name repeatedly within one request → one HTTP call.
    public Task<City> GetLocationOfCity(string cityName)
        => scopedCache.GetOrAdd(cityName, key => innerClient.GetLocationOfCity(key));

    // Reverse-geocode rarely changes → cache across requests for the process lifetime.
    public Task<City> GetNameOfCity(City city)
        => singletonCache.GetOrAdd(city, key => innerClient.GetNameOfCity(key));

    // Uncached calls just pass through to the inner client.
    public Task<double[]> GetDurationMatrixByTollRoad(List<City> cities)
        => innerClient.GetDurationMatrixByTollRoad(cities);
}
```

Wire the decorator with Scrutor, right after registering the real client
(`DreamTravel.GeolocationDataClients/ModuleInstaller.cs`):

```csharp
services.AddHTTPClient<IGoogleHTTPClient, GoogleHTTPClient, GoogleHTTPOptions>("Google");
services.Decorate(typeof(IGoogleHTTPClient), typeof(GoogleHTTPClientCachingDecorator));
```

Consumers keep depending on `IGoogleHTTPClient` — they never see the cache.

### ISingletonCache — long-lived in-memory cache

Singleton-scoped. Entries survive across requests until expiration. Reach for it when the
underlying value is stable and shared by everyone — like the reverse-geocode above:

```csharp
public Task<City> GetNameOfCity(City city)
    => singletonCache.GetOrAdd(city, key => innerClient.GetNameOfCity(key));
```

### IScopedCache<TKey, TItem> — request-scoped deduplication

Scoped to the DI scope (typically one HTTP request). Prevents duplicate calls within the same
request — multiple chapters resolving the same city name share a single lookup:

```csharp
public Task<City> GetLocationOfCity(string cityName)
    => scopedCache.GetOrAdd(cityName, key => innerClient.GetLocationOfCity(key));
```

### IRedisCache — Redis-backed distributed cache

Same `GetOrAdd` contract, same decorator shape — swap the in-process tier for Redis when the
value should be shared **across instances**. If Redis is down, the factory is called and the
result returned without caching (fail-open):

```csharp
public class GoogleHTTPClientCachingDecorator(
    IGoogleHTTPClient innerClient,
    IRedisCache cache) : IGoogleHTTPClient
{
    public Task<City> GetLocationOfCity(string cityName)
        => cache.GetOrAdd(cityName, key => innerClient.GetLocationOfCity(key));
}
```

---

## Testing

### Local tier

No fixture needed — `IMemoryCache` works out of the box in unit tests.

### Redis tier

Use **`SolTechnology.Core.Redis.Testing`** with Testcontainers:

```csharp
RedisFixture = new RedisFixture();
await RedisFixture.InitializeAsync();

var configuration = new TestConfigurationBuilder()
    .AddJsonFile("appsettings.tests.json")
    .Override("Redis:HostName", RedisFixture.HostName)
    .Override("Redis:Enabled", "true")
    .Build();

await RedisFixture.FlushAsync();    // between-test reset
await RedisFixture.DisposeAsync();  // teardown
```

Full reference: [Redis.Testing.md](Redis.Testing.md).

---

## API Reference

### ModuleInstaller

| Method | Description |
|--------|-------------|
| `AddLocalCache(CacheConfiguration?)` | Registers `ISingletonCache`, `IScopedCache<,>` |
| `AddDistributedCache(DistributedCacheConfiguration)` | Registers `IRedisCache` (Redis-backed) |
| `AddLocalLock()` | Registers `IDistributedLockService` (in-process, for local dev / single instance) |
| `AddDistributedLock()` | Registers `IDistributedLockService` (Redis `SET NX`, requires `AddDistributedCache`) |
| `AddLocalIdempotency(TimeSpan?)` | Registers `IIdempotencyStore` (in-process, for local dev / single instance) |
| `AddDistributedIdempotency(TimeSpan?)` | Registers `IIdempotencyStore` (Redis `SET NX`, requires `AddDistributedCache`) |

### Unified Interface

```csharp
// ISingletonCache
Task<TItem> GetOrAdd<TKey, TItem>(TKey key, Func<TKey, Task<TItem>> factory);

// IRedisCache
Task<TItem> GetOrAdd<TKey, TItem>(TKey key, Func<TKey, Task<TItem>> factory);

// IScopedCache<TKey, TItem>
Task<TItem> GetOrAdd(TKey key, Func<TKey, Task<TItem>> factory);
```

Same shape everywhere. Key is any serializable object. Factory is called on cache miss.

---

## Distributed Lock

`IDistributedLockService` provides cross-instance mutual exclusion backed by the **same Redis**
that powers the distributed cache. Two registration methods mirror the cache pattern:

```csharp
// Local dev / single instance — in-process SemaphoreSlim, no Redis needed
services.AddLocalLock();

// Production — Redis SET NX (requires AddDistributedCache to be called first)
services.AddDistributedCache(redisConfig);
services.AddDistributedLock();
```

### Usage

```csharp
public class SettlementPoller(IDistributedLockService locks)
{
    public async Task PollAsync(CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireLockAsync(
            "settlement/batch-process", expiry: TimeSpan.FromMinutes(5), ct);

        if (handle is null)
            return; // another instance holds the lock — skip this cycle

        // Only one instance executes this at a time
        await ProcessBatchAsync(ct);
    }
}
```

### Contract

| Aspect | Behaviour |
|---|---|
| **Success** | Returns `IAsyncDisposable` — disposing releases the lock immediately. |
| **Lock held by another** | Returns `null` — never blocks. |
| **Redis unavailable** | Returns `null` + logs Warning — **never throws** (fail-open like the cache). |
| **Caller cancellation** | May throw `OperationCanceledException` — the only exception path. |
| **Expiry (TTL)** | Lock auto-releases after `expiry` even if the holder crashes. Prevents deadlocks. |
| **Fencing** | Each acquisition generates a unique token — release only deletes the key if the token still matches (prevents releasing someone else's lock after expiry). |

### Guard-rails

- **Never throws into a host loop.** A backend failure degrades to `null`, not an exception. Your
  polling loop stays alive.
- **Include tenant/principal in the lock name** where relevant — e.g. `$"settlements/{tenantId}/batch"`.
  The library prefixes with the configured `InstanceName` automatically.
- **Keep expiry honest.** Set it longer than the expected work duration but short enough that a crash
  doesn't hold the lock forever. 2–5× the expected duration is a good heuristic.

### How it works (Redis `SET NX`)

```
SET "SolTechnology:lock:settlement/batch-process" "<unique-guid>" NX EX 300
```

- `NX` = only set if **N**ot e**X**ists (atomic mutual exclusion)
- `EX 300` = auto-expire after 300s (crash safety)
- Release = Lua script: `DEL key` only if value still matches (fencing)

No external libraries needed — it's `StackExchange.Redis` under the hood (already a dependency of
the distributed cache tier).

### When to use (Lock vs Cache)

| You need… | Use |
|---|---|
| "Don't **compute** this twice" (same result, expensive to produce) | Cache (`GetOrAdd`) |
| "Don't **do** this twice at the same time" (side-effectful, one winner) | Lock (`TryAcquireLockAsync`) |

**Concrete examples:**
- ✅ Lock: "Only one instance polls for new settlements every 30s" (leader election)
- ✅ Lock: "Only one instance runs the nightly cleanup job" (singleton background task)
- ✅ Lock: "Don't process the same payment event concurrently on two pods" (deduplication at execution level)
- ❌ Lock for: "Don't call the Google API twice for the same city" → that's a cache miss, use `GetOrAdd`

---

## Idempotency Store

`IIdempotencyStore` manages request deduplication keys — ensuring that retried HTTP requests with the
same `Idempotency-Key` header produce the **exact same response** without re-executing the handler.
Backed by the **same Redis** that powers the cache and lock.

### The problem it solves

```
Client sends: POST /payments  (Idempotency-Key: abc-123)
Server processes payment → 201 Created
Network drops before client gets the response
Client retries: POST /payments  (Idempotency-Key: abc-123)
Without idempotency: payment executes AGAIN → double charge 💸
With idempotency: server replays the stored 201 response → safe ✅
```

### Registration

```csharp
// Local dev / single instance — in-process ConcurrentDictionary with TTL
services.AddLocalIdempotency();

// Production — Redis SET NX (requires AddDistributedCache to be called first)
services.AddDistributedCache(redisConfig);
services.AddDistributedIdempotency();

// Optional: custom TTL (default 24h)
services.AddDistributedIdempotency(ttl: TimeSpan.FromHours(48));
```

### Contract

| Operation | Behaviour |
|---|---|
| **`TryAddAsync(key)`** | Atomically reserves a key. Returns `true` if this caller won (key is new); `false` if already held by another request. |
| **`GetAsync(key)`** | Gets the stored response for replay, or `null` if the handler hasn't completed yet. |
| **`SetResponseAsync(key, response)`** | Persists the handler's response (status + headers + body) under the key. |
| **`RemoveAsync(key)`** | Removes the key — used when the handler throws (allows retry). |

### Guard-rails

- **Never store 5xx.** A transient failure must not become the permanent "answer" for that key.
  The middleware (in `Core.Api`) calls `RemoveAsync` on exception — the client can safely retry.
- **Atomic reservation.** Two identical requests hitting two pods simultaneously: only one wins the
  `SET NX` — the other waits and replays the stored response.
- **TTL auto-expiry.** Keys don't grow forever — expired after the configured TTL (default 24h).
- **Fail-open.** If Redis is unavailable, `TryAddAsync` returns `true` (lets the request through)
  rather than blocking. Better to risk a duplicate than to reject all traffic.

### When to use (Idempotency vs Lock vs Cache)

| You need… | Use |
|---|---|
| "Don't **compute** this twice" | Cache |
| "Don't **do** this twice at the same time" | Lock |
| "Don't **do** this twice ever (even across retries minutes apart)" | Idempotency |

**Concrete examples:**
- ✅ Idempotency: "Client retries a payment POST — serve the same 201 without double-charging"
- ✅ Idempotency: "Mobile app loses network after submit — user hits retry — same result"
- ❌ Idempotency for: "Two pods processing the same queue message simultaneously" → that's a Lock
- ❌ Idempotency for: "Don't call an expensive API twice for the same input" → that's a Cache

### Middleware recipe (copy into your app)

The library provides the **store** — the middleware is a ~30-line snippet in your app:

```csharp
app.Use(async (context, next) =>
{
    var store = context.RequestServices.GetRequiredService<IIdempotencyStore>();
    var key = context.Request.Headers["Idempotency-Key"].FirstOrDefault();

    if (string.IsNullOrEmpty(key))
    {
        await next(); // no key — pass through
        return;
    }

    // Check if we already have a stored response
    var stored = await store.GetAsync(key, context.RequestAborted);
    if (stored is not null)
    {
        context.Response.StatusCode = stored.StatusCode;
        foreach (var h in stored.Headers) context.Response.Headers[h.Key] = h.Value;
        await context.Response.Body.WriteAsync(stored.Body, context.RequestAborted);
        return;
    }

    // Try to reserve the key
    if (!await store.TryAddAsync(key, context.RequestAborted))
    {
        context.Response.StatusCode = 409; // Conflict — someone else is processing
        return;
    }

    // Capture the response
    var originalBody = context.Response.Body;
    using var buffer = new MemoryStream();
    context.Response.Body = buffer;

    try
    {
        await next();

        // Only store 2xx responses — never cache failures
        if (context.Response.StatusCode is >= 200 and < 300)
        {
            buffer.Seek(0, SeekOrigin.Begin);
            var response = new StoredResponse
            {
                StatusCode = context.Response.StatusCode,
                Headers = context.Response.Headers
                    .ToDictionary(h => h.Key, h => h.Value.ToArray()),
                Body = buffer.ToArray()
            };
            await store.SetResponseAsync(key, response, context.RequestAborted);
        }
        else
        {
            await store.RemoveAsync(key, context.RequestAborted); // allow retry on non-2xx
        }

        buffer.Seek(0, SeekOrigin.Begin);
        await buffer.CopyToAsync(originalBody, context.RequestAborted);
    }
    catch
    {
        await store.RemoveAsync(key); // handler threw — allow retry
        throw;
    }
    finally
    {
        context.Response.Body = originalBody;
    }
});
```

