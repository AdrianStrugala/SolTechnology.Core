using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NUnit.Framework;

namespace SolTechnology.Core.Cache.Tests;

[TestFixture]
public class LocalDistributedLockServiceTests
{
    private static readonly TimeSpan Expiry = TimeSpan.FromSeconds(30);

    private FakeTimeProvider _time = null!;
    private LocalDistributedLockService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider();
        _sut = new LocalDistributedLockService(NullLogger<LocalDistributedLockService>.Instance, _time);
    }

    [Test]
    public async Task Dispose_Twice_DoesNotReleaseOtherHolder()
    {
        // Arrange
        var first = await _sut.TryAcquireLock("job", Expiry);
        await first!.DisposeAsync();
        var second = await _sut.TryAcquireLock("job", Expiry);

        // Act
        await first.DisposeAsync();
        var third = await _sut.TryAcquireLock("job", Expiry);

        // Assert
        second.Should().NotBeNull();
        third.Should().BeNull();
    }

    [Test]
    public async Task TryAcquire_AfterExpiry_Succeeds()
    {
        // Arrange
        var expired = await _sut.TryAcquireLock("job", Expiry);
        var beforeExpiry = await _sut.TryAcquireLock("job", Expiry);
        _time.Advance(Expiry + TimeSpan.FromSeconds(1));

        // Act
        var afterExpiry = await _sut.TryAcquireLock("job", Expiry);
        await expired!.DisposeAsync();
        var whileTakenOver = await _sut.TryAcquireLock("job", Expiry);

        // Assert
        beforeExpiry.Should().BeNull();
        afterExpiry.Should().NotBeNull();
        whileTakenOver.Should().BeNull("disposing the expired handle must not release the new holder");
    }

    [Test]
    public async Task Release_Uncontended_RemovesEntry()
    {
        // Arrange
        var handle = await _sut.TryAcquireLock("job", Expiry);
        var heldWhileAcquired = _sut.HeldLockCount;

        // Act
        await handle!.DisposeAsync();

        // Assert
        heldWhileAcquired.Should().Be(1);
        _sut.HeldLockCount.Should().Be(0);
    }

    [Test]
    public async Task TryAcquire_Cancelled_Throws()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var act = async () => await _sut.TryAcquireLock("job", Expiry, cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        _sut.HeldLockCount.Should().Be(0);
    }

    [Test]
    public async Task TryAcquireLock_LogsBracketedLockKey()
    {
        // Arrange
        var logger = new CapturingLogger<LocalDistributedLockService>();
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(
            _ => _time.GetUtcNow(),
            _ => _time.GetUtcNow(),
            _ => throw new InvalidOperationException("clock failure"));
        var sut = new LocalDistributedLockService(logger, clock);

        // Act
        var handle = await sut.TryAcquireLock("job", Expiry);
        await sut.TryAcquireLock("job", Expiry);
        await handle!.DisposeAsync();
        var failed = await sut.TryAcquireLock("job", Expiry);

        // Assert
        failed.Should().BeNull();
        logger.Entries.Select(entry => (entry.Level, entry.Template)).Should().Equal(
            (LogLevel.Debug, "Local lock acquired: [{LockKey}]"),
            (LogLevel.Debug, "Local lock not acquired: [{LockKey}] (held by another caller)"),
            (LogLevel.Debug, "Local lock released: [{LockKey}]"),
            (LogLevel.Warning, "Local lock acquisition failed for [{LockKey}] — degrading to null"));
        logger.Entries.Should().AllSatisfy(entry => entry.Properties["LockKey"].Should().Be("job"));
    }

    [TestCaseSource(typeof(LockExpiryCases), nameof(LockExpiryCases.OutOfRange))]
    public async Task TryAcquireLock_ExpiryOutOfRange_Throws(TimeSpan expiry)
    {
        // Act
        var act = async () => await _sut.TryAcquireLock("job", expiry);

        // Assert
        (await act.Should().ThrowAsync<ArgumentOutOfRangeException>()).Which.ParamName.Should().Be("expiry");
        _sut.HeldLockCount.Should().Be(0);
    }

    [Test]
    public async Task TryAcquireLock_MaximumExpiry_Acquires()
    {
        // Act
        var handle = await _sut.TryAcquireLock("job", LockExpiryCases.Maximum);

        // Assert
        handle.Should().NotBeNull();
    }
}
