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