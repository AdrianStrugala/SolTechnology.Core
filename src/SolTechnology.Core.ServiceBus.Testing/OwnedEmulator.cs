using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using SolTechnology.Core.Testing.Containers;
using Testcontainers.MsSql;
using Testcontainers.ServiceBus;

namespace SolTechnology.Core.ServiceBus.Testing;

/// <summary>
/// The emulator container with the MSSQL sidecar and network it needs, created and removed together. Disposing a
/// <c>Testcontainers.ServiceBus</c> container alone leaves the sidecar it created running, so this type owns both.
/// With reuse they get the well-known names of <see cref="ReusedEmulator"/> and are kept.
/// </summary>
internal sealed class OwnedEmulator : IAsyncDisposable
{
    internal const int AmqpPort = 5672;
    private const string DatabaseImage = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";
    private const string DatabaseNetworkAlias = "database-container";

    private readonly bool _reuse;
    private readonly INetwork _network;
    private readonly MsSqlContainer _database;

    internal OwnedEmulator(string containerName, string? configFilePath, bool reuse)
    {
        _reuse = reuse;
        var networkBuilder = new NetworkBuilder().WithCleanUp(!reuse);
        // AutoRemove deletes the sidecar on stop even when Ryuk is disabled, like the emulator itself.
        var databaseBuilder = new MsSqlBuilder(DatabaseImage).WithCleanUp(!reuse).WithAutoRemove(!reuse);
        if (reuse)
        {
            networkBuilder = networkBuilder.WithName(ReusedEmulator.NetworkName(containerName));
            databaseBuilder = databaseBuilder.WithName(ReusedEmulator.DatabaseName(containerName));
        }

        _network = networkBuilder.Build();
        _database = databaseBuilder.WithNetwork(_network).WithNetworkAliases(DatabaseNetworkAlias).Build();
        var builder = ServiceBusInstanceBuilder.CreateBuilder(configFilePath)
            .WithMsSqlContainer(_network, _database, DatabaseNetworkAlias);
        Container = (reuse ? builder.WithName(containerName) : builder).Build();
    }

    internal ServiceBusContainer Container { get; }

    /// <summary>Starts the sidecar and the emulator, waits for AMQP and returns the connection string.</summary>
    internal async Task<string> Start(CancellationToken ct)
    {
        await Container.StartAsync(ct).ConfigureAwait(false);

        // Testcontainers' built-in wait only confirms the TCP port is open. Gate on the AMQP probe.
        await ContainerLifecycleHelper.WaitForAmqpReady(Container.GetMappedPublicPort(AmqpPort)).ConfigureAwait(false);
        return Container.GetConnectionString();
    }

    internal async Task EnsureRunning(CancellationToken ct)
    {
        await ContainerLifecycleHelper.EnsureRunning(_database.Id, ct).ConfigureAwait(false);
        await ContainerLifecycleHelper.EnsureRunning(Container.Id, ct).ConfigureAwait(false);
    }

    /// <summary>Removes the emulator, the sidecar and the network; keeps them when reuse is enabled.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_reuse)
        {
            return;
        }

        await Container.DisposeAsync().ConfigureAwait(false);
        await _database.DisposeAsync().ConfigureAwait(false);

        // Without Ryuk (TESTCONTAINERS_RYUK_DISABLED) DisposeAsync keeps the network; delete it explicitly.
        await _network.DeleteAsync().ConfigureAwait(false);
        await _network.DisposeAsync().ConfigureAwait(false);
    }
}
