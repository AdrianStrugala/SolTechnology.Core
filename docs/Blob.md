### Overview

The SolTechnology.Core.BlobStorage library provides minimum functionality needed for Azure Blob storage connection. It handles needed services registration and configuration and additionally provides BlobContainerClientExtensions methods for convinient read and write operation.

### Registration

For installing the library, reference **SolTechnology.Core.BlobStorage** nuget package and invoke **AddBlobStorage()** service collection extension method:

```csharp
services.AddBlobStorage();
```

### Configuration

1) The first option is to create an appsettings.json section:

```csharp
  "Configuration": {
    "BlobStorage": {
      "ConnectionString": "UseDevelopmentStorage=true"
    }
  }
```

2) Alternatevely the same settings can be provided by optional parameter during registration:

```csharp
var blobStorageConfiguration = new BlobStorageConfiguration
{
    ConnectionString = "UseDevelopmentStorage=true"
};
services.AddBlobStorage(blobStorageConfiguration);
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