using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache.Tests;

/// <summary>
/// Unit tests for <see cref="RedisIdempotencyStore"/> — focused on the fail-open contract
/// (a Redis outage must never block the request) and the basic reserve/replay round-trip.
/// </summary>
public sealed class RedisIdempotencyStoreTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static RedisIdempotencyStore CreateSut(IConnectionMultiplexer redis)
    {
        redis.IsConnected.Returns(true);
        return new RedisIdempotencyStore(
            redis,
            Options.Create(new DistributedCacheConfiguration { InstanceName = "test:" }),
            NullLogger<RedisIdempotencyStore>.Instance,
            TimeSpan.FromHours(1));
    }

    [Test]
    public async Task TryAdd_RedisDown_FailsOpen_ReturnsTrue()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Returns(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));
        var sut = CreateSut(redis);

        var result = await sut.TryAdd("key-1");

        // Fail-open: better to risk a duplicate than to reject all traffic during an outage.
        result.Should().BeTrue();
    }

    [Test]
    public async Task Get_RedisDown_ReturnsNull()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Returns(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));
        var sut = CreateSut(redis);

        var result = await sut.Get("key-1");

        result.Should().BeNull();
    }

    [Test]
    public async Task Remove_RedisDown_DoesNotThrow()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Returns(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));
        var sut = CreateSut(redis);

        var act = async () => await sut.Remove("key-1");

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task TryAdd_WhenKeyIsNew_ReturnsTrue()
    {
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(true);
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        var sut = CreateSut(redis);

        var result = await sut.TryAdd("key-1");

        result.Should().BeTrue();
    }

    [Test]
    public async Task TryAdd_WhenKeyAlreadyHeld_ReturnsFalse()
    {
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(false); // SET NX returns false when the key already exists
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        var sut = CreateSut(redis);

        var result = await sut.TryAdd("key-1");

        result.Should().BeFalse();
    }

    [Test]
    public async Task Get_WhenReservedButNoResponse_ReturnsNull()
    {
        var db = Substitute.For<IDatabase>();
        db.StringGetAsync(Arg.Any<RedisKey>()).Returns(RedisValue.EmptyString);
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        var sut = CreateSut(redis);

        var result = await sut.Get("key-1");

        result.Should().BeNull();
    }

    [Test]
    public async Task Get_WhenResponseStored_ReturnsDeserializedResponse()
    {
        var stored = new StoredResponse
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string[]> { ["X-Test"] = ["v"] },
            Body = "ok"u8.ToArray()
        };
        var json = JsonSerializer.Serialize(stored, JsonOptions);

        var db = Substitute.For<IDatabase>();
        db.StringGetAsync(Arg.Any<RedisKey>()).Returns(new RedisValue(json));
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        var sut = CreateSut(redis);

        var result = await sut.Get("key-1");

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Body.Should().Equal(stored.Body);
    }

    [Test]
    public async Task Operations_RedisNotConnected_FailOpenWithoutCallingRedis()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.IsConnected.Returns(false);
        var logger = new CapturingLogger<RedisIdempotencyStore>();
        var sut = new RedisIdempotencyStore(
            redis,
            Options.Create(new DistributedCacheConfiguration { InstanceName = "test:" }),
            logger,
            TimeSpan.FromHours(1));

        // Act
        var reserved = await sut.TryAdd("key-1");
        var stored = await sut.Get("key-1");
        await sut.SetResponse("key-1", new StoredResponse { StatusCode = 200, Headers = [], Body = [] });
        await sut.Remove("key-1");

        // Assert
        reserved.Should().BeTrue();
        stored.Should().BeNull();
        redis.DidNotReceiveWithAnyArgs().GetDatabase();
        logger.Entries.Select(entry => (entry.Level, entry.Template, entry.Properties["Operation"])).Should().Equal(
            (LogLevel.Warning, "Redis not connected; skipping [{Operation}] for [{Key}]", "TryAdd"),
            (LogLevel.Warning, "Redis not connected; skipping [{Operation}] for [{Key}]", "Get"),
            (LogLevel.Warning, "Redis not connected; skipping [{Operation}] for [{Key}]", "SetResponse"),
            (LogLevel.Warning, "Redis not connected; skipping [{Operation}] for [{Key}]", "Remove"));
        logger.Entries.Should().AllSatisfy(entry => entry.Properties["Key"].Should().Be("key-1"));
    }

    [Test]
    public async Task Operations_RedisDown_LogBracketedIdempotencyKey()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Returns(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));
        redis.IsConnected.Returns(true);
        var logger = new CapturingLogger<RedisIdempotencyStore>();
        var sut = new RedisIdempotencyStore(
            redis,
            Options.Create(new DistributedCacheConfiguration { InstanceName = "test:" }),
            logger,
            TimeSpan.FromHours(1));

        // Act
        await sut.TryAdd("key-1");
        await sut.Get("key-1");
        await sut.SetResponse("key-1", new StoredResponse { StatusCode = 200, Headers = [], Body = [] });
        await sut.Remove("key-1");

        // Assert
        logger.Entries.Select(entry => (entry.Level, entry.Template)).Should().Equal(
            (LogLevel.Warning, "Idempotency TryAdd failed for [{IdempotencyKey}] — allowing request through"),
            (LogLevel.Warning, "Idempotency Get failed for [{IdempotencyKey}]"),
            (LogLevel.Warning, "Idempotency SetResponse failed for [{IdempotencyKey}] — response will not be replayed on retry"),
            (LogLevel.Warning, "Idempotency Remove failed for [{IdempotencyKey}] — key will expire via TTL"));
        logger.Entries.Should().AllSatisfy(entry => entry.Properties["IdempotencyKey"].Should().Be("key-1"));
    }
}
