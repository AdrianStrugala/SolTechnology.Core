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

    /// <summary>
    /// Removes the emulator, the sidecar and the network, each even when removing another fails; keeps them when
    /// reuse is enabled.
    /// </summary>
    /// <exception cref="AggregateException">One or more resources could not be removed.</exception>
    public async ValueTask DisposeAsync()
    {
        if (_reuse)
        {
            return;
        }

        var failures = new List<Exception>();
        await Attempt(() => Container.DisposeAsync().AsTask(), failures).ConfigureAwait(false);
        await Attempt(() => _database.DisposeAsync().AsTask(), failures).ConfigureAwait(false);
        await Attempt(async () =>
        {
            // Without Ryuk (TESTCONTAINERS_RYUK_DISABLED) DisposeAsync keeps this network; delete it explicitly.
            await _network.DeleteAsync().ConfigureAwait(false);
            await _network.DisposeAsync().ConfigureAwait(false);
        }, failures).ConfigureAwait(false);

        if (failures.Count > 0)
        {
            throw new AggregateException("Could not remove every Service Bus emulator resource.", failures);
        }
    }

    private static async Task Attempt(Func<Task> removal, List<Exception> failures)
    {
        try
        {
            await removal().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            failures.Add(ex);
        }
    }
}
