# SolTechnology.Core.Blob.Testing

Integration-testing fixture for apps backed by Azure Blob Storage (e.g. `SolTechnology.Core.Blob`
consumers): a [Testcontainers](https://dotnet.testcontainers.org/)-backed `AzuriteFixture` that boots an
[Azurite](https://github.com/Azure/Azurite) container (the Azure Storage emulator) and hands back the
connection string your app already wires.

> **Azure-specific by design** — there is no LocalStack / S3 path.
> Reference from test projects only — not needed at runtime in production assemblies.

## What's in the box

| Member | Purpose |
|---|---|
| `AzuriteFixture` | Boots an `azurite` container, exposes `ConnectionString`, `CreateBlobContainerAsync` / `ClearAsync`, honours the shared reuse policy. |
| `ConnectionString` | Azure Storage connection string for the running Azurite instance. |
| `CreateBlobContainerAsync(name)` | Returns a `BlobContainerClient`, creating the blob container if needed. |
| `ClearAsync()` | Deletes every blob container — the between-test reset when the container is reused. |
| `WithNetwork(network, alias)` | Attach to a docker network to share with other fixtures. |

## Usage

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
await container.UploadBlobAsync("key", BinaryData.FromString("payload"));

// In a per-test teardown, when the container is reused:
await AzuriteFixture.ClearAsync();

// Assembly-level [OneTimeTearDown]
await AzuriteFixture.DisposeAsync();   // no-op when TESTCONTAINERS_REUSE=true
```

## Container lifetime & reuse

The fixture defers to `SolTechnology.Core.Testing`'s `TestContainersContext`:

- **Within a run** — boot once in `[OneTimeSetUp]`; every test reuses the same container for free.
- **Across runs** — set `TESTCONTAINERS_REUSE=true` to keep the container alive between runs
  (stable name + `WithReuse(true)`, and `ContainerLifecycleHelper.EnsureRunningAsync` restarts it if it
  was stopped externally); `DisposeAsync()` becomes a no-op. CI stays hermetic by default.
- **Between tests** — call `ClearAsync()` to drop all blob containers without restarting Azurite.

