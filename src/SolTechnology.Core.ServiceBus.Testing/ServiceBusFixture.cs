using System.Collections.Concurrent;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using SolTechnology.Core.Testing.Containers;
using Testcontainers.MsSql;
using Testcontainers.ServiceBus;

namespace SolTechnology.Core.ServiceBus.Testing;

/// <summary>
/// Fixture for the Azure Service Bus emulator container — the most lifetime-sensitive of the testing
/// companions. Supports multiple named instances so different test fixtures can own their own emulator.
/// </summary>
/// <remarks>
/// <para>
/// The emulator persists its state in SQL Server. The fixture creates that MSSQL sidecar and the network
/// they share, passes them to <c>Testcontainers.ServiceBus</c>, and removes all three in
/// <see cref="DisposeAsync"/>; disposing the emulator container alone would leave the sidecar running.
/// </para>
/// <para>
/// Each instance owns its state: two fixtures with the same name do not share a connection string, and
/// disposing one does not break the other. When <see cref="TestContainersContext.ReuseContainers"/> is
/// enabled, the emulator, sidecar and network get well-known names and stay between runs; a later run finds
/// them by name, starts them if stopped, and re-reads the mapped port to rebuild the connection string.
/// </para>
/// <para>
/// Readiness is gated on <see cref="ContainerLifecycleHelper.WaitForAmqpReadyAsync"/> — a TCP-accept alone
/// causes <c>NullReferenceException</c> in <c>AmqpTransportInitiator</c>; do not regress this.
/// </para>
/// </remarks>
public sealed class ServiceBusFixture : IAsyncDisposable
{
    // Reuse mode pins one Docker container per name, so fixtures sharing a name start it one at a time.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Semaphores = new();

    private const int ServiceBusPort = 5672;
    private const string DatabaseImage = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";
    private const string DatabaseNetworkAlias = "database-container";

    private readonly string? _configFilePath;
    private readonly string _containerName;
    private string? _connectionString;
    private MsSqlContainer? _database;
    private INetwork? _network;

    /// <summary>The Testcontainers-managed container (only set when created fresh; null when reused by name).</summary>
    public ServiceBusContainer? Container { get; private set; }

    /// <param name="containerName">Stable container name (used for reuse-by-name). Suffix with <paramref name="instanceName"/> for multiple emulators.</param>
    /// <param name="instanceName">Optional suffix for running multiple named emulator instances side by side.</param>
    /// <param name="configFilePath">Optional path to a custom emulator topology; null uses the bundled default.</param>
    public ServiceBusFixture(
        string containerName = "soltech-servicebus-emulator",
        string? instanceName = null,
        string? configFilePath = null)
    {
        _configFilePath = configFilePath;
        _containerName = string.IsNullOrEmpty(instanceName) ? containerName : $"{containerName}-{instanceName}";
    }

    /// <summary>The emulator connection string, whether the container was created fresh or reused.</summary>
    public string ConnectionString => _connectionString
        ?? throw new InvalidOperationException("ServiceBusFixture not initialized. Call InitializeAsync first.");

    public async Task InitializeAsync()
    {
        var ct = CancellationToken.None;
        var semaphore = Semaphores.GetOrAdd(_containerName, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_connectionString is not null)
            {
                // Already initialized; the container may have been stopped externally (e.g. via Docker Desktop).
                await EnsureRunning(ct).ConfigureAwait(false);
                return;
            }

            if (TestContainersContext.ReuseContainers)
            {
                _connectionString = await ReusedEmulator.TryReuse(_containerName, ServiceBusPort, ct).ConfigureAwait(false);
                if (_connectionString is not null)
                {
                    return;
                }

                await ReusedEmulator.Remove(_containerName, ct).ConfigureAwait(false);
            }

            await StartEmulator(ct).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>Removes the emulator, its MSSQL sidecar and their network; keeps them when reuse is enabled.</summary>
    public async ValueTask DisposeAsync()
    {
        if (TestContainersContext.ReuseContainers)
        {
            return;
        }

        _connectionString = null;
        if (Container is not null)
        {
            await Container.DisposeAsync().ConfigureAwait(false);
            Container = null;
        }

        if (_database is not null)
        {
            await _database.DisposeAsync().ConfigureAwait(false);
            _database = null;
        }

        if (_network is not null)
        {
            // Without Ryuk (TESTCONTAINERS_RYUK_DISABLED) DisposeAsync keeps the network; delete it explicitly.
            await _network.DeleteAsync().ConfigureAwait(false);
            await _network.DisposeAsync().ConfigureAwait(false);
            _network = null;
        }
    }

    private async Task StartEmulator(CancellationToken ct)
    {
        var reuse = TestContainersContext.ReuseContainers;
        var networkBuilder = new NetworkBuilder().WithCleanUp(!reuse);
        // AutoRemove deletes the sidecar on stop even when Ryuk is disabled, like the emulator itself.
        var databaseBuilder = new MsSqlBuilder(DatabaseImage).WithCleanUp(!reuse).WithAutoRemove(!reuse);
        if (reuse)
        {
            networkBuilder = networkBuilder.WithName(ReusedEmulator.NetworkName(_containerName));
            databaseBuilder = databaseBuilder.WithName(ReusedEmulator.DatabaseName(_containerName));
        }

        _network = networkBuilder.Build();
        _database = databaseBuilder.WithNetwork(_network).WithNetworkAliases(DatabaseNetworkAlias).Build();

        var builder = ServiceBusInstanceBuilder.CreateBuilder(_configFilePath)
            .WithMsSqlContainer(_network, _database, DatabaseNetworkAlias);
        Container = (reuse ? builder.WithName(_containerName) : builder).Build();

        try
        {
            await Container.StartAsync(ct).ConfigureAwait(false);

            // Testcontainers' built-in wait only confirms the TCP port is open. Gate on the AMQP probe.
            await ContainerLifecycleHelper.WaitForAmqpReadyAsync(Container.GetMappedPublicPort(ServiceBusPort)).ConfigureAwait(false);
            _connectionString = Container.GetConnectionString();
        }
        catch
        {
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task EnsureRunning(CancellationToken ct)
    {
        if (Container is not null)
        {
            if (_database is not null)
            {
                await ContainerLifecycleHelper.EnsureRunningAsync(_database.Id, ct).ConfigureAwait(false);
            }

            await ContainerLifecycleHelper.EnsureRunningAsync(Container.Id, ct).ConfigureAwait(false);
        }
        else if (TestContainersContext.ReuseContainers)
        {
            _connectionString = await ReusedEmulator.TryReuse(_containerName, ServiceBusPort, ct).ConfigureAwait(false)
                ?? _connectionString;
        }
    }
}
