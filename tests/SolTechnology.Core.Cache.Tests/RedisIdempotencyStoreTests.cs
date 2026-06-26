using System.Text.Json;
using FluentAssertions;
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

    private static RedisIdempotencyStore CreateSut(IConnectionMultiplexer redis) =>
        new(
            redis,
            Options.Create(new DistributedCacheConfiguration { InstanceName = "test:" }),
            NullLogger<RedisIdempotencyStore>.Instance,
            TimeSpan.FromHours(1));

    [Test]
    public async Task TryAddAsync_RedisDown_FailsOpen_ReturnsTrue()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Returns(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));
        var sut = CreateSut(redis);

        var result = await sut.TryAddAsync("key-1");

        // Fail-open: better to risk a duplicate than to reject all traffic during an outage.
        result.Should().BeTrue();
    }

    [Test]
    public async Task GetAsync_RedisDown_ReturnsNull()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Returns(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));
        var sut = CreateSut(redis);

        var result = await sut.GetAsync("key-1");

        result.Should().BeNull();
    }

    [Test]
    public async Task RemoveAsync_RedisDown_DoesNotThrow()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Returns(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));
        var sut = CreateSut(redis);

        var act = async () => await sut.RemoveAsync("key-1");

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task TryAddAsync_WhenKeyIsNew_ReturnsTrue()
    {
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(true);
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        var sut = CreateSut(redis);

        var result = await sut.TryAddAsync("key-1");

        result.Should().BeTrue();
    }

    [Test]
    public async Task TryAddAsync_WhenKeyAlreadyHeld_ReturnsFalse()
    {
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(false); // SET NX returns false when the key already exists
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        var sut = CreateSut(redis);

        var result = await sut.TryAddAsync("key-1");

        result.Should().BeFalse();
    }

    [Test]
    public async Task GetAsync_WhenReservedButNoResponse_ReturnsNull()
    {
        var db = Substitute.For<IDatabase>();
        db.StringGetAsync(Arg.Any<RedisKey>()).Returns(RedisValue.EmptyString);
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        var sut = CreateSut(redis);

        var result = await sut.GetAsync("key-1");

        result.Should().BeNull();
    }

    [Test]
    public async Task GetAsync_WhenResponseStored_ReturnsDeserializedResponse()
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

        var result = await sut.GetAsync("key-1");

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Body.Should().Equal(stored.Body);
    }
}

