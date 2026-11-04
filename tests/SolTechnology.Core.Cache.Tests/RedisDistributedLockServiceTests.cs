using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache.Tests;

[TestFixture]
public class RedisDistributedLockServiceTests
{
    private const string LockKey = "test:lock:job";
    private static readonly TimeSpan Expiry = TimeSpan.FromSeconds(30);

    [Test]
    public async Task TryAcquireLock_LogsBracketedLockKey()
    {
        // Arrange
        var down = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down");
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(true, false, true);
        db.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(_ => Task.FromResult(RedisResult.Create((RedisValue)1)), _ => throw down);
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(_ => db, _ => db, _ => throw down, _ => db);
        var logger = new CapturingLogger<RedisDistributedLockService>();
        var sut = new RedisDistributedLockService(
            redis, Options.Create(new DistributedCacheConfiguration { InstanceName = "test:" }), logger);

        // Act
        await (await sut.TryAcquireLock("job", Expiry))!.DisposeAsync();
        var held = await sut.TryAcquireLock("job", Expiry);
        var failed = await sut.TryAcquireLock("job", Expiry);
        await (await sut.TryAcquireLock("job", Expiry))!.DisposeAsync();

        // Assert
        held.Should().BeNull();
        failed.Should().BeNull();
        logger.Entries.Select(entry => (entry.Level, entry.Template)).Should().Equal(
            (LogLevel.Debug, "Lock acquired: [{LockKey}]"),
            (LogLevel.Debug, "Lock released: [{LockKey}]"),
            (LogLevel.Debug, "Lock not acquired: [{LockKey}] (held by another instance)"),
            (LogLevel.Warning, "Lock acquisition failed for [{LockKey}] — degrading to null"),
            (LogLevel.Debug, "Lock acquired: [{LockKey}]"),
            (LogLevel.Warning, "Lock release failed for [{LockKey}] — will expire via TTL"));
        logger.Entries.Should().AllSatisfy(entry => entry.Properties["LockKey"].Should().Be(LockKey));
    }

    [TestCaseSource(typeof(LockExpiryCases), nameof(LockExpiryCases.OutOfRange))]
    public async Task TryAcquireLock_ExpiryOutOfRange_Throws(TimeSpan expiry)
    {
        // Arrange
        var db = Substitute.For<IDatabase>();
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        var sut = new RedisDistributedLockService(
            redis, Options.Create(new DistributedCacheConfiguration { InstanceName = "test:" }),
            new CapturingLogger<RedisDistributedLockService>());

        // Act
        var act = async () => await sut.TryAcquireLock("job", expiry);

        // Assert
        (await act.Should().ThrowAsync<ArgumentOutOfRangeException>()).Which.ParamName.Should().Be("expiry");
        db.ReceivedCalls().Should().BeEmpty();
    }
}
