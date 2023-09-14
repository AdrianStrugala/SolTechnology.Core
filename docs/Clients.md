### Overview

The SolTechnology.Core.ApiClient library provides minimum functionality needed for API calls over HTTP. It handles needed services registration and configuration and a result provides HttpClients.

### Registration

For installing the library, reference **SolTechnology.Core.ApiClient** nuget package and invoke **AddApiClient<IApiClient, ApiClient>("http-client-name")** service collection extension method:

```csharp
services.AddApiClient<IFootballDataApiClient, FootballDataApiClient>("football-data");  //has to match the name from configuration
```

### Configuration

1) The first option is to create an appsettings.json section:

```csharp
  "Configuration": {
    "ApiClients": {
      "football-data": {
        "BaseAddress": "http://api.football-data.org",
        "Headers": [
          {
            "Name": "X-Auth-Token",
            "Value": ""
          }
        ]
      },
      "api-football": {
        "BaseAddress": "https://api-football-v1.p.rapidapi.com",
        "Headers": [
          {
            "Name": "x-rapidapi-host",
            "Value": ""
          },
          {
            "Name": "x-rapidapi-key",
            "Value": ""
          }
        ]
      }
    }
  }
```

2) Alternatevely the same settings can be provided by optional parameter during registration:

```csharp
    var footballDataApiConfiguration = new ApiClientConfiguration
    {
        BaseAddress = "http://api.football-data.org",
        Name = "football-data",
        Headers = new List<Header>
        {
            new Header
            {
                Name = "X-Auth-Token",
                Value = ""
            }
        }
    };

    services.AddApiClient<IFootballDataApiClient, FootballDataApiClient>("football-data", footballDataApiConfiguration);  //has to match the name from configuration
```


### Usage

1) Inject HttpClient to previously registered class

```csharp
      public FootballDataApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
```

2) Invoke the http method

```csharp
    var apiResult = await _httpClient.GetAsync<MatchModel>($"v2/matches/{matchApiId}");
```

3) The library supports Get, Post, Put and Delete http operations as well as Json and Avro data formats.