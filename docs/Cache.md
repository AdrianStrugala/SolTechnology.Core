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

```csharp
services.AddLocalCache();
```

Or with explicit configuration:

```csharp
services.AddLocalCache(new CacheConfiguration
{
    ExpirationMode = ExpirationMode.Absolute,
    ExpirationSeconds = 600
});
```

### Distributed cache (Redis)

```csharp
services.AddDistributedCache(new DistributedCacheConfiguration
{
    ConnectionString = "localhost:6379",
    InstanceName = "MyApp:",
    ExpirationSeconds = 300
});
```

### Both tiers together

```csharp
services.AddLocalCache();
services.AddDistributedCache(configuration.GetSection("Redis").Get<DistributedCacheConfiguration>()!);
```

> Use `ISingletonCache` for hot in-process data and `IRedisCache` for shared cross-instance data.

---

## Configuration

### CacheConfiguration (local)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ExpirationMode` | `ExpirationMode` | `Absolute` | `Absolute` or `Sliding` |
| `ExpirationSeconds` | `int` | `1200` (20 min) | Time before entry expires |

Can be provided via `appsettings.json`:

```json
{
  "Configuration": {
    "CacheConfiguration": {
      "ExpirationMode": "Absolute",
      "ExpirationSeconds": 300
    }
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

### ISingletonCache — long-lived in-memory cache

Singleton-scoped. Entries survive across requests until expiration.

```csharp
public class GetWeatherHandler(ISingletonCache cache, IWeatherApi api)
{
    public Task<Weather> Handle(GetWeatherQuery query)
    {
        return cache.GetOrAdd(query.CityId, api.FetchWeather);
    }
}
```

### IScopedCache<TKey, TItem> — request-scoped deduplication

Scoped to the DI scope (typically one HTTP request). Prevents duplicate calls within the same request.

```csharp
public class SyncPlayer(IScopedCache<int, Player> cache, IPlayerApi api)
{
    public Task<Player> Resolve(int playerId)
    {
        return cache.GetOrAdd(playerId, api.GetPlayerById);
    }
}
```

### IRedisCache — Redis-backed distributed cache

Same `GetOrAdd` contract. If Redis is down, factory is called and result returned without caching.

```csharp
public class GetProductHandler(IRedisCache cache, IProductRepository repo)
{
    public Task<Product> Handle(int productId)
    {
        return cache.GetOrAdd(productId, id => repo.GetById(id));
    }
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
