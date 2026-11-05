using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.Testing;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace SolTechnology.Core.Cache.Tests.Redis;

[TestFixture]
public class RedisOutageTests
{
    private const string Image = "redis:7-alpine";
    private const int RedisPort = 6379;
    private const int PortAttempts = 3;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private RedisContainer? _redis;
    private ServiceProvider? _provider;

    [SetUp]
    public async Task SetUp()
    {
        _redis = await StartOnFixedHostPort();
        var services = new ServiceCollection().AddLogging();
        services.AddSolDistributedCache(new DistributedCacheConfiguration
        {
            ConnectionString = _redis.GetConnectionString(),
            InstanceName = "outage:"
        });
        services.AddSolDistributedLock();
        services.AddSolDistributedIdempotency();
        _provider = services.BuildServiceProvider();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }

        if (_redis is not null)
        {
            await _redis.DisposeAsync();
        }
    }

    [Test]
    public async Task GetOrAdd_RedisStoppedThenStarted_FailsFastThenCachesAgain()
    {
        // Arrange
        var redis = _redis!;
        var provider = _provider!;
        var cache = provider.GetRequiredService<IRedisCache>();
        var locks = provider.GetRequiredService<IDistributedLockService>();
        var idempotency = provider.GetRequiredService<IIdempotencyStore>();
        var connection = provider.GetRequiredService<IConnectionMultiplexer>();
        var cachedBeforeOutage = await IsCaching(cache, "before-outage");
        await redis.StopAsync();
        var disconnected = await Retry.Until(
            () => Task.FromResult(!connection.IsConnected), isDown => isDown, TimeSpan.FromSeconds(10), PollInterval);

        // Act
        var outage = Stopwatch.StartNew();
        var fromFactory = await cache.GetOrAdd("during-outage", _ => Task.FromResult("from factory"));
        var handle = await locks.TryAcquireLock("job", TimeSpan.FromSeconds(30));
        var reserved = await idempotency.TryAdd("request-1");
        outage.Stop();
        await redis.StartAsync();
        var cachedAfterOutage = await Retry.Until(
            () => IsCaching(cache, "after-outage"), isCaching => isCaching, TimeSpan.FromSeconds(20), PollInterval);

        // Assert
        cachedBeforeOutage.Should().BeTrue();
        disconnected.Should().BeTrue();
        fromFactory.Should().Be("from factory");
        handle.Should().BeNull();
        reserved.Should().BeTrue();
        outage.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
        cachedAfterOutage.Should().BeTrue();
    }

    private static async Task<bool> IsCaching(IRedisCache cache, string key)
    {
        var factoryCalls = 0;
        await cache.GetOrAdd(key, _ => Task.FromResult(++factoryCalls));
        await cache.GetOrAdd(key, _ => Task.FromResult(++factoryCalls));
        return factoryCalls < 2;
    }

    // A fixed host port survives the stop/start; a random binding would move Redis to a new port.
    private static async Task<RedisContainer> StartOnFixedHostPort()
    {
        for (var attempt = 1; ; attempt++)
        {
            var container = new RedisBuilder(Image)
                .WithPortBinding(FreeHostPort(), RedisPort)
                .Build();
            try
            {
                await container.StartAsync();
                return container;
            }
            catch (Exception)
            {
                await container.DisposeAsync();
                if (attempt == PortAttempts)
                {
                    throw;
                }
            }
        }
    }

    private static int FreeHostPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
