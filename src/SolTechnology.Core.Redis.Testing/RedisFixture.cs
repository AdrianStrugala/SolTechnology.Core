using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using SolTechnology.Core.Testing.Containers;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace SolTechnology.Core.Redis.Testing;

/// <summary>
/// Spins up a Redis container and exposes the connection details in the shape apps wire today
/// (<c>Redis:HostName</c> = <c>host:port</c>, plus a full StackExchange.Redis
/// <see cref="ConnectionString"/>). No coupling to <c>SolTechnology.Core.Cache</c> runtime types — the fixture only
/// provides a running Redis + connection string.
/// </summary>
/// <remarks>
/// Boot once from the consumer's assembly-level <c>[OneTimeSetUp]</c> (within-run reuse is free);
/// across-run reuse is opt-in via <c>TESTCONTAINERS_REUSE</c> (see <see cref="TestContainersContext"/>),
/// which flips the container to a stable name with <c>WithReuse(true)</c> and makes
/// <see cref="DisposeAsync"/> a no-op. Use <see cref="Flush"/> for a between-test reset when the
/// container is reused.
/// </remarks>
/// <param name="image">The Redis image; defaults to <c>redis:7-alpine</c>.</param>
/// <param name="containerName">The container name used when <c>TESTCONTAINERS_REUSE</c> keeps it between runs.</param>
public sealed class RedisFixture(string? image = null, string containerName = "soltech-redis") : IAsyncDisposable
{
    private const string DefaultImage = "redis:7-alpine";
    private const int InternalPort = 6379;

    private INetwork? _network;
    private string? _networkAlias;
    private RedisContainer? _container;
    private ConnectionMultiplexer? _multiplexer;

    private RedisContainer Container =>
        _container ?? throw new InvalidOperationException("Container not started. Call Initialize first.");

    /// <summary><c>host:port</c> — the value apps bind to <c>Redis:HostName</c>. Valid after <see cref="Initialize"/>.</summary>
    public string HostName => $"{Container.Hostname}:{Container.GetMappedPublicPort(InternalPort)}";

    /// <summary>Full StackExchange.Redis connection string. Valid after <see cref="Initialize"/>.</summary>
    public string ConnectionString => Container.GetConnectionString();

    /// <summary>Attach the container to a docker network (e.g. to share with other fixtures). Call before <see cref="Initialize"/>.</summary>
    public RedisFixture WithNetwork(INetwork network, string? alias = null)
    {
        _network = network;
        _networkAlias = alias;
        return this;
    }

    /// <summary>Starts the Redis container, or reuses the one kept by an earlier run.</summary>
    public async Task Initialize(CancellationToken ct = default)
    {
        var builder = new RedisBuilder(image ?? DefaultImage)
            .WithPortBinding(InternalPort, assignRandomHostPort: true)
            .WithCleanUp(!TestContainersContext.ReuseContainers);

        if (TestContainersContext.ReuseContainers)
        {
            builder = builder.WithName(containerName).WithReuse(true);
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
    }

    /// <summary>Flushes all keys — the between-test reset when the container is reused.</summary>
    public async Task Flush()
    {
        if (_multiplexer is null)
        {
            // FLUSHALL is a server (admin) command — StackExchange.Redis rejects it unless AllowAdmin is set.
            var options = ConfigurationOptions.Parse(ConnectionString);
            options.AllowAdmin = true;
            _multiplexer = await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false);
        }

        foreach (var endpoint in _multiplexer.GetEndPoints())
        {
            var server = _multiplexer.GetServer(endpoint);
            await server.FlushAllDatabasesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Closes the admin connection and removes the container, unless <c>TESTCONTAINERS_REUSE</c> keeps it.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_multiplexer is not null)
        {
            await _multiplexer.DisposeAsync().ConfigureAwait(false);
        }

        // Honour the reuse policy: leave the container running when reuse is enabled.
        if (_container is not null && !TestContainersContext.ReuseContainers)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}


