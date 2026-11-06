## SolTechnology.Core.Blob

Typed Azure Blob Storage reads and writes in JSON or Avro. `IBlobConnectionFactory` hands out one cached
`BlobContainerClient` per container and creates the container on first use, and the `ReadFromBlob` and `WriteToBlob`
extensions serialize your objects and record the format in the blob's metadata.

### Features

- **Typed reads and writes** — `WriteToBlob(name, item)` and `ReadFromBlob<T>(name)` on any `BlobContainerClient`.
- **JSON or Avro** — JSON by default for readability, Avro with the Brotli codec for smaller blobs; the reader picks the
  format from the blob's metadata.
- **Atomic format marker** — the format key is uploaded with the content in one call, so no reader sees a blob
  without it, and the blob's other metadata keys are kept on overwrite.
- **Containers created once** — the first `GetConnection` for a name creates the container, once, even under
  concurrent calls; a write to a container deleted since then creates it again.
- **Cancellation** — every read and write takes a `CancellationToken`.
- **Test fixture** — `SolTechnology.Core.Blob.Testing` runs Azurite in a container for component tests.

### Registration

Register the module with its configuration:

```csharp
var blobConfiguration = builder.Configuration.GetSection("Blob").Get<BlobConfiguration>()!;
builder.Services.AddSolBlob(blobConfiguration);
```

### Configuration

| `BlobConfiguration` | Default | Purpose |
|---|---|---|
| `ConnectionString` | — (required) | Azure Storage connection string for every container client the factory creates. It must be non-empty and parsable as an Azure Storage connection string; otherwise the host stops at start with `OptionsValidationException`. |

Bind it from the `Blob` section, and keep the account key in an environment variable (`Blob__ConnectionString`) or a
secret store rather than in `appsettings.json`:

```json
{
  "Blob": {
    "ConnectionString": "UseDevelopmentStorage=true"
  }
}
```

### Usage

#### Repositories

Inject `IBlobConnectionFactory`, get the container client once, and read and write through the extensions:

```csharp
using Azure.Storage.Blobs;
using SolTechnology.Core.Blob;
using SolTechnology.Core.Blob.Connection;

public record PlayerStatistics(int Id, int Goals);

public class PlayerStatisticsRepository(IBlobConnectionFactory blobConnectionFactory)
{
    private readonly BlobContainerClient _container = blobConnectionFactory.GetConnection("playerstatistics");

    public Task Save(PlayerStatistics statistics, CancellationToken ct) =>
        _container.WriteToBlob(statistics.Id.ToString(), statistics, SerializationFormat.Avro, ct);

    public Task<PlayerStatistics> Get(int playerId, CancellationToken ct) =>
        _container.ReadFromBlob<PlayerStatistics>(playerId.ToString(), ct);
}
```

#### Serialization formats

`WriteToBlob(name, item, ct)` writes JSON; pass a `SerializationFormat` to choose:

| `SerializationFormat` | Content | `SerializationFormat` metadata value |
|---|---|---|
| `Json` (default) | UTF-8 JSON through `System.Text.Json` | `json` |
| `Avro` | Avro with the Brotli codec through AvroConvert | `avro` |

`ReadFromBlob<T>` deserializes by the metadata value and reads a blob without it as JSON, so blobs uploaded by other
tools as JSON read too.

#### Write and read behaviour

| Situation | Behaviour |
|---|---|
| Blob exists | `WriteToBlob` overwrites it and keeps its other metadata keys. |
| Blob missing | `WriteToBlob` creates it. |
| Container deleted after `GetConnection` | `WriteToBlob` creates it again once and retries the upload. |
| Blob or container missing on read | `ReadFromBlob` throws `RequestFailedException` with status 404. |
| `SerializationFormat` not a defined value | `WriteToBlob` throws `ArgumentOutOfRangeException` before sending any request. |
| Token cancelled | Both throw `OperationCanceledException`. |

### Testing

The companion package `SolTechnology.Core.Blob.Testing` provides `AzuriteFixture`, an Azurite container that hands back
a connection string for `AddSolBlob`:

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.Blob;
using SolTechnology.Core.Blob.Connection;
using SolTechnology.Core.Blob.Testing;

public class PlayerStatisticsBlobTests
{
    private AzuriteFixture _azurite = null!;

    [OneTimeSetUp]
    public async Task StartAzurite()
    {
        _azurite = new AzuriteFixture();
        await _azurite.InitializeAsync();
    }

    [TearDown]
    public Task Clear() => _azurite.ClearAsync();

    [OneTimeTearDown]
    public async Task StopAzurite() => await _azurite.DisposeAsync();

    [Test]
    public async Task WriteToBlob_ThenReadFromBlob_ReturnsTheSameStatistics()
    {
        // Arrange
        using var provider = new ServiceCollection()
            .AddSolBlob(new BlobConfiguration { ConnectionString = _azurite.ConnectionString })
            .BuildServiceProvider();
        var container = provider.GetRequiredService<IBlobConnectionFactory>().GetConnection("playerstatistics");
        var statistics = new PlayerStatistics(7, 42);

        // Act
        await container.WriteToBlob("7", statistics, SerializationFormat.Avro);

        // Assert
        (await container.ReadFromBlob<PlayerStatistics>("7")).Should().Be(statistics);
    }
}
```

Reference the package from test projects only. Container lifetime and `TESTCONTAINERS_REUSE` follow
[theQuality.md → Container lifetime & reuse](theQuality.md#container-lifetime--reuse); see also
[Blob.Testing.md](Blob.Testing.md).

### Conventions

- Import `SolTechnology.Core.Blob` for the extensions; since 2.0 they are no longer in `Azure.Storage.Blobs`.
- Inject `IBlobConnectionFactory`; its implementation is internal.
- Get a container client once per repository and reuse it; the factory caches one per name.
- Pass the caller's `CancellationToken` to every read and write.
- Keep the `SerializationFormat` metadata key: removing it makes the blob read as JSON.
- Return domain types from repositories, not `BlobClient` or `BinaryData`.
