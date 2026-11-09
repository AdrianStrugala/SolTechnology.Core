using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace SolTechnology.Core.Cache.Tests.Redis;

[TestFixture]
public class RedisLockReleaseTests
{
    private CapturingLogger<RedisDistributedLockService> _logger = null!;
    private RedisDistributedLockService _sut = null!;

    [SetUp]
    public async Task SetUp()
    {
        await RedisTestEnvironment.Redis.Flush();
        _logger = new CapturingLogger<RedisDistributedLockService>();
        _sut = new RedisDistributedLockService(
            RedisTestEnvironment.RawConnection,
            Options.Create(new DistributedCacheConfiguration { InstanceName = "release:" }),
            _logger);
    }

    [Test]
    public async Task Dispose_AfterExpiryAndTakeover_WarnsAndKeepsNewHolder()
    {
        // Arrange
        var expired = await _sut.TryAcquireLock("job", TimeSpan.FromMilliseconds(200));
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        var current = await _sut.TryAcquireLock("job", TimeSpan.FromSeconds(30));

        // Act
        await expired!.DisposeAsync();

        // Assert
        current.Should().NotBeNull();
        RedisTestEnvironment.RawKeys().Should().Equal("release:lock:job");
        _logger.Entries.Select(entry => (entry.Level, entry.Template)).Should().Equal(
            (LogLevel.Debug, "Lock acquired: [{LockKey}]"),
            (LogLevel.Debug, "Lock acquired: [{LockKey}]"),
            (LogLevel.Warning, "Lock [{LockKey}] expired before release"));
    }

    [Test]
    public async Task Dispose_Twice_ReleasesOnceWithoutWarning()
    {
        // Arrange
        var handle = await _sut.TryAcquireLock("job", TimeSpan.FromSeconds(30));

        // Act
        await handle!.DisposeAsync();
        await handle.DisposeAsync();

        // Assert
        RedisTestEnvironment.RawKeys().Should().BeEmpty();
        _logger.Entries.Select(entry => (entry.Level, entry.Template)).Should().Equal(
            (LogLevel.Debug, "Lock acquired: [{LockKey}]"),
            (LogLevel.Debug, "Lock released: [{LockKey}]"));
    }
}
