using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache.Tests.Redis;

[TestFixture]
public class DistributedCacheRegistrationTests
{
    [SetUp]
    public async Task SetUp() => await RedisTestEnvironment.Redis.Flush();

    [Test]
    public async Task AddSolDistributedCache_UsesSingleMultiplexer()
    {
        // Arrange
        await using var provider = RedisTestEnvironment.BuildProvider("app:");
        var distributedCache = provider.GetRequiredService<IDistributedCache>();
        var factory = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value.ConnectionMultiplexerFactory;

        // Act
        await distributedCache.SetStringAsync("probe", "value");
        var roundTrip = await distributedCache.GetStringAsync("probe");
        var cacheMultiplexer = factory is null ? null : await factory();

        // Assert
        roundTrip.Should().Be("value");
        cacheMultiplexer.Should().BeSameAs(provider.GetRequiredService<IConnectionMultiplexer>());
    }
}
