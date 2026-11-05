using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache.Tests;

[TestFixture]
public class RedisCacheConnectionTests
{
    private const string CacheKey = "System.String:System.Int32:\"wroclaw\"";

    [Test]
    public async Task GetOrAdd_RedisNotConnected_RunsFactoryWithoutCallingRedis()
    {
        // Arrange
        var distributedCache = Substitute.For<IDistributedCache>();
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.IsConnected.Returns(false);
        var logger = new CapturingLogger<RedisCache>();
        var sut = CreateSut(distributedCache, redis, logger);

        // Act
        var result = await sut.GetOrAdd("wroclaw", _ => Task.FromResult(640_000));

        // Assert
        result.Should().Be(640_000);
        distributedCache.ReceivedCalls().Should().BeEmpty();
        logger.Entries.Select(entry => (entry.Level, entry.Template, entry.Properties["Operation"], entry.Properties["Key"]))
            .Should().Equal((LogLevel.Warning, "Redis not connected; skipping [{Operation}] for [{Key}]", "GetOrAdd", CacheKey));
    }

    [Test]
    public async Task GetOrAdd_ConnectionLostBeforeWrite_SkipsWrite()
    {
        // Arrange
        var distributedCache = Substitute.For<IDistributedCache>();
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.IsConnected.Returns(true, false);
        var logger = new CapturingLogger<RedisCache>();
        var sut = CreateSut(distributedCache, redis, logger);

        // Act
        var result = await sut.GetOrAdd("wroclaw", _ => Task.FromResult(640_000));

        // Assert
        result.Should().Be(640_000);
        await distributedCache.Received(1).GetAsync(CacheKey, Arg.Any<CancellationToken>());
        await distributedCache.DidNotReceiveWithAnyArgs().SetAsync(default!, default!, default!, default);
        logger.Entries.Should().ContainSingle().Which.Level.Should().Be(LogLevel.Warning);
    }

    private static RedisCache CreateSut(
        IDistributedCache distributedCache, IConnectionMultiplexer redis, CapturingLogger<RedisCache> logger) =>
        new(distributedCache, redis, Options.Create(new DistributedCacheConfiguration { InstanceName = "app:" }), logger);
}
