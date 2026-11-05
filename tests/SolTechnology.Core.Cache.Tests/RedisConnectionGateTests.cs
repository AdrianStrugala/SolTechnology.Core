using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NUnit.Framework;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache.Tests;

[TestFixture]
public class RedisConnectionGateTests
{
    [Test]
    public void IsClosed_RedisNotConnected_LogsWarningOncePerOperationAndInterval()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.IsConnected.Returns(false);
        var logger = new CapturingLogger<RedisConnectionGateTests>();
        var time = new FakeTimeProvider();
        var sut = new RedisConnectionGate(redis, logger, time);

        // Act
        var closed = new List<bool> { sut.IsClosed("TryAdd", "a"), sut.IsClosed("TryAdd", "b"), sut.IsClosed("Get", "c") };
        time.Advance(RedisConnectionGate.WarningInterval - TimeSpan.FromTicks(1));
        closed.Add(sut.IsClosed("TryAdd", "d"));
        time.Advance(TimeSpan.FromTicks(1));
        closed.Add(sut.IsClosed("TryAdd", "e"));

        // Assert
        closed.Should().AllSatisfy(isClosed => isClosed.Should().BeTrue());
        logger.Entries.Should().AllSatisfy(entry =>
            entry.Template.Should().Be("Redis not connected; skipping [{Operation}] for [{Key}]"));
        logger.Entries.Select(entry => (entry.Level, entry.Properties["Operation"], entry.Properties["Key"])).Should().Equal(
            (LogLevel.Warning, "TryAdd", "a"),
            (LogLevel.Debug, "TryAdd", "b"),
            (LogLevel.Warning, "Get", "c"),
            (LogLevel.Debug, "TryAdd", "d"),
            (LogLevel.Warning, "TryAdd", "e"));
    }

    [Test]
    public void IsClosed_RedisConnected_ReturnsFalseWithoutLogging()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.IsConnected.Returns(true);
        var logger = new CapturingLogger<RedisConnectionGateTests>();
        var sut = new RedisConnectionGate(redis, logger);

        // Act
        var closed = sut.IsClosed("TryAdd", "a");

        // Assert
        closed.Should().BeFalse();
        logger.Entries.Should().BeEmpty();
    }
}
