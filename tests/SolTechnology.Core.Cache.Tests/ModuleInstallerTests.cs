using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.Cache.HealthChecks;

namespace SolTechnology.Core.Cache.Tests;

[TestFixture]
public class ModuleInstallerTests
{
    private const string UnreachableRedis = "localhost:1,abortConnect=false,connectTimeout=100";

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task AddSolDistributedCache_MissingInstanceName_HostStartFails(string? instanceName)
    {
        // Arrange
        var configuration = new DistributedCacheConfiguration { ConnectionString = UnreachableRedis };
        if (instanceName is not null)
        {
            configuration.InstanceName = instanceName;
        }
        using var host = BuildHost(services => services.AddSolDistributedCache(configuration));

        // Act
        var start = async () => await host.StartAsync();

        // Assert
        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain(nameof(DistributedCacheConfiguration.InstanceName));
    }

    [Test]
    public async Task AddSolLocalCache_ConfigWithoutExpirationSeconds_HostStartFails()
    {
        // Arrange
        var configuration = new CacheConfiguration { ExpirationMode = ExpirationMode.Sliding };
        using var host = BuildHost(services => services.AddSolLocalCache(configuration));

        // Act
        var start = async () => await host.StartAsync();

        // Assert
        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain(nameof(CacheConfiguration.ExpirationSeconds));
    }

    [Test]
    public async Task AddSolDistributedCache_RedisDownAtStart_FailsOpen()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSolDistributedCache(new DistributedCacheConfiguration
        {
            ConnectionString = "localhost:1,connectTimeout=100,asyncTimeout=500,syncTimeout=500",
            InstanceName = "redis-down:"
        });
        services.AddSolDistributedLock();
        services.AddSolDistributedIdempotency();
        services.AddHealthChecks().AddSolRedisHealthCheck(timeout: TimeSpan.FromSeconds(1));
        await using var provider = services.BuildServiceProvider();
        var factoryCalls = 0;

        // Act
        var cached = await provider.GetRequiredService<IRedisCache>()
            .GetOrAdd("wroclaw", _ => Task.FromResult(++factoryCalls));
        var handle = await provider.GetRequiredService<IDistributedLockService>()
            .TryAcquireLock("job", TimeSpan.FromSeconds(30));
        var reserved = await provider.GetRequiredService<IIdempotencyStore>().TryAdd("request-1");
        var health = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        // Assert
        cached.Should().Be(1);
        factoryCalls.Should().Be(1);
        handle.Should().BeNull();
        reserved.Should().BeTrue();
        health.Status.Should().Be(HealthStatus.Unhealthy);
    }

    private static IHost BuildHost(Action<IServiceCollection> configure)
    {
        var builder = Host.CreateApplicationBuilder();
        configure(builder.Services);
        return builder.Build();
    }
}
