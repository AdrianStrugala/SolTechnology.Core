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

