### Overview

The SolTechnology.Core.Cache library provides functionality needed for task caching. It handles needed services registration and configuration. It relies on IMemoryCache. The use case is to cache parts of the code which are time consuming and the result does not change. Ex: external http calls for non-changing data. 

### Registration

For installing the library, reference **SolTechnology.Core.Cache** nuget package and invoke **AddCache()** service collection extension method:

```csharp
services.AddCache();
```

### Configuration

1) The first option is to create an appsettings.json section:

```csharp
  "Configuration": {
    "CacheConfiguration": {
        "ExpirationMode": "Absolute or Sliding",
        "ExpirationSeconds": 300
     }
  }
```

2) Alternatevely the same settings can be provided by optional parameter during registration:

```csharp
var cacheConfiguration = new CacheConfiguration
{
    ExpirationMode = "Absolute",
	ExpirationSeconds= 300
};

builder.Services.AddCache(cacheConfiguration);
```

3) If not provided, the default configuration will be applied, which is Sliding ExpirationMode and 300 seconds expiration time. That means if the cache was not hit in 5 mins, it will be cleared.

### Usage

1) Inject ILazyTaskCache into your service 

```csharp
        public SyncPlayer(
            IFootballDataApiClient footballDataApiClient,
            IPlayerRepository playerRepository,
            IApiFootballApiClient apiFootballApiClient,
            ILazyTaskCache lazyTaskCache)
        {
            _footballDataApiClient = footballDataApiClient;
            _playerRepository = playerRepository;
            _apiFootballApiClient = apiFootballApiClient;
            _lazyTaskCache = lazyTaskCache;
        }
```

2) Cache repeatable operation and it's key

```csharp
         var clientPlayer = await _lazyTaskCache.GetOrAdd(playerIdMap.FootballDataId, _footballDataApiClient.GetPlayerById);
```

3) The key is supposed to be a complex object (ex: command or query), to avoid returning incorrect cache item

### Testing

When the cache is backed by **Redis**, the companion package **`SolTechnology.Core.Redis.Testing`**
provides `RedisFixture` — a [Testcontainers](https://dotnet.testcontainers.org/)-backed Redis container
for component tests. Reference it from test projects only. Full reference: [Redis.Testing.md](Redis.Testing.md).

```csharp
// Assembly-level [OneTimeSetUp]
RedisFixture = new RedisFixture();
await RedisFixture.InitializeAsync();

var configuration = new TestConfigurationBuilder()
    .AddJsonFile("appsettings.tests.json")
    .Override("Redis:HostName", RedisFixture.HostName)
    .Override("Redis:Enabled", "true")
    .Build();

await RedisFixture.FlushAsync();    // between-test reset (clears all keys)
await RedisFixture.DisposeAsync();  // no-op when TESTCONTAINERS_REUSE=true
```

> The in-memory `IMemoryCache` path needs no fixture — it is exercised directly in unit tests. The
> `RedisFixture` is only for suites that bind the cache to a real Redis instance. Container lifetime /
> reuse follows the shared model in
> [theQuality.md → Container lifetime & reuse](theQuality.md#container-lifetime--reuse).
