### Overview

The SolTechnology.Core.Blob library provides minimum functionality needed for Azure Blob storage connection. It handles needed services registration and configuration and additionally provides BlobContainerClientExtensions methods for convinient read and write operation.

### Registration

For installing the library, reference **SolTechnology.Core.Blob** nuget package and invoke **AddSolBlob()** service collection extension method:

```csharp
services.AddSolBlob(blobConfiguration);
```

### Configuration

1) The first option is to create an appsettings.json section:

```csharp
  "Configuration": {
    "Blob": {
      "ConnectionString": "UseDevelopmentStorage=true"
    }
  }
```

2) Alternatevely the same settings can be provided by optional parameter during registration:

```csharp
var blobConfiguration = new BlobConfiguration
{
    ConnectionString = "UseDevelopmentStorage=true"
};
services.AddSolBlob(blobConfiguration);
```


### Usage

1) Inject IBlobConnectionFactory and create blobContainerClient using chosen container name

```csharp
       public PlayerStatisticsRepository(IBlobConnectionFactory blobConnectionFactory)
        {
            _client = blobConnectionFactory.CreateConnection(ContainerName);
        }
```

2) Invoke read and write operation

```csharp
      await _client.WriteToBlob(blobName, content);
      var result = await _client.ReadFromBlob<ContentType>(blobName);
```

3) The libary supports two serialization formats:
- json as default for high redability
- avro for data compression and cost reduction

### Testing

The companion package **`SolTechnology.Core.Blob.Testing`** provides `AzuriteFixture` — a
[Testcontainers](https://dotnet.testcontainers.org/)-backed [Azurite](https://github.com/Azure/Azurite)
container (the Azure Storage emulator) for component tests. Reference it from test projects only. Full reference:
[Blob.Testing.md](Blob.Testing.md).

```csharp
// Assembly-level [OneTimeSetUp]
AzuriteFixture = new AzuriteFixture();
await AzuriteFixture.InitializeAsync();

var configuration = new TestConfigurationBuilder()
    .AddJsonFile("appsettings.tests.json")
    .Override("Blob:ConnectionString", AzuriteFixture.ConnectionString)
    .Build();

// Arrange a blob container in a test:
var container = await AzuriteFixture.CreateBlobContainerAsync("dossier");

await AzuriteFixture.ClearAsync();     // between-test reset (drops all blob containers)
await AzuriteFixture.DisposeAsync();   // no-op when TESTCONTAINERS_REUSE=true
```

Container lifetime / reuse follows the shared model in
[theQuality.md → Container lifetime & reuse](theQuality.md#container-lifetime--reuse).
