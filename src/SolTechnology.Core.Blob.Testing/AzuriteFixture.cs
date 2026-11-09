using Azure.Storage.Blobs;
using DotNet.Testcontainers.Networks;
using SolTechnology.Core.Testing.Containers;
using Testcontainers.Azurite;

namespace SolTechnology.Core.Blob.Testing;

/// <summary>
/// Spins up an <see href="https://github.com/Azure/Azurite">Azurite</see> container (the Azure Storage
/// emulator) and exposes its <see cref="ConnectionString"/>. The Azure-specific companion of
/// <c>SolTechnology.Core.Blob</c>
/// </summary>
/// <remarks>
/// <para>
/// Boot once from the consumer's assembly-level <c>[OneTimeSetUp]</c> (within-run reuse is free);
/// across-run reuse is opt-in via <c>TESTCONTAINERS_REUSE</c> (see <see cref="TestContainersContext"/>),
/// which flips the container to a stable name with <c>WithReuse(true)</c> and makes
/// <see cref="DisposeAsync"/> a no-op. Use <see cref="Clear"/> for a between-test reset when the
/// container is reused. No hand-rolled reuse cache — the shared lifetime model owns that.
/// </para>
/// </remarks>
public sealed class AzuriteFixture : IAsyncDisposable
{
    // 3.37.0 is the first Azurite that accepts service version 2026-06-06 of Azure.Storage.Blobs 12.29.0.
    private const string DefaultImage = "mcr.microsoft.com/azure-storage/azurite:3.37.0";

    private readonly string? _image;
    private readonly string _containerName;

    private INetwork? _network;
    private string? _networkAlias;
    private AzuriteContainer? _container;

    /// <param name="image">The Azurite image; defaults to <c>mcr.microsoft.com/azure-storage/azurite:3.37.0</c>.</param>
    /// <param name="containerName">The container name used when <c>TESTCONTAINERS_REUSE</c> keeps it between runs.</param>
    public AzuriteFixture(string? image = null, string containerName = "soltech-azurite")
    {
        _image = image;
        _containerName = containerName;
    }

    private AzuriteContainer Container =>
        _container ?? throw new InvalidOperationException("Container not started. Call Initialize first.");

    /// <summary>Azure Storage connection string for the running Azurite instance. Valid after <see cref="Initialize"/>.</summary>
    public string ConnectionString => Container.GetConnectionString();

    /// <summary>Attach the container to a docker network (e.g. to share with other fixtures). Call before <see cref="Initialize"/>.</summary>
    public AzuriteFixture WithNetwork(INetwork network, string? alias = null)
    {
        _network = network;
        _networkAlias = alias;
        return this;
    }

    /// <summary>Starts the Azurite container, or reuses the one kept by an earlier run.</summary>
    public async Task Initialize(CancellationToken ct = default)
    {
        var builder = new AzuriteBuilder(_image ?? DefaultImage)
            .WithCleanUp(!TestContainersContext.ReuseContainers);

        if (TestContainersContext.ReuseContainers)
        {
            builder = builder.WithName(_containerName).WithReuse(true).WithAutoRemove(false);
        }

        if (_network is not null)
        {
            builder = builder.WithNetwork(_network);
            if (!string.IsNullOrEmpty(_networkAlias))
            {
                builder = builder.WithNetworkAliases(_networkAlias);
            }
        }

        _container = builder.Build();
        await _container.StartAsync(ct).ConfigureAwait(false);

        // When the container is reused it may have been stopped externally while a static flag still
        // says "initialized" — make sure it is genuinely running before handing it back.
        if (TestContainersContext.ReuseContainers)
        {
            await ContainerLifecycleHelper.EnsureRunning(_container.Id, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Returns a <see cref="BlobContainerClient"/> for <paramref name="containerName"/>, creating the blob container if needed.</summary>
    public async Task<BlobContainerClient> CreateBlobContainer(string containerName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        var client = new BlobServiceClient(ConnectionString).GetBlobContainerClient(containerName);
        await client.CreateIfNotExistsAsync(cancellationToken: ct).ConfigureAwait(false);
        return client;
    }

    /// <summary>Deletes every blob container — the between-test reset when the Azurite container is reused.</summary>
    public async Task Clear(CancellationToken ct = default)
    {
        var service = new BlobServiceClient(ConnectionString);
        await foreach (var container in service.GetBlobContainersAsync(cancellationToken: ct).ConfigureAwait(false))
        {
            await service.DeleteBlobContainerAsync(container.Name, cancellationToken: ct).ConfigureAwait(false);
        }
    }

    /// <summary>Removes the container, unless <c>TESTCONTAINERS_REUSE</c> keeps it for the next run.</summary>
    public async ValueTask DisposeAsync()
    {
        // Honour the reuse policy: leave the container running when reuse is enabled.
        if (_container is not null && !TestContainersContext.ReuseContainers)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}

