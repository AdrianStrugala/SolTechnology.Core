using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
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
        var first = await _sut.TryAcquireLockAsync("job", Expiry);
        await first!.DisposeAsync();
        var second = await _sut.TryAcquireLockAsync("job", Expiry);

        // Act
        await first.DisposeAsync();
        var third = await _sut.TryAcquireLockAsync("job", Expiry);

        // Assert
        second.Should().NotBeNull();
        third.Should().BeNull();
    }

    [Test]
    public async Task TryAcquire_AfterExpiry_Succeeds()
    {
        // Arrange
        var expired = await _sut.TryAcquireLockAsync("job", Expiry);
        var beforeExpiry = await _sut.TryAcquireLockAsync("job", Expiry);
        _time.Advance(Expiry + TimeSpan.FromSeconds(1));

        // Act
        var afterExpiry = await _sut.TryAcquireLockAsync("job", Expiry);
        await expired!.DisposeAsync();
        var whileTakenOver = await _sut.TryAcquireLockAsync("job", Expiry);

        // Assert
        beforeExpiry.Should().BeNull();
        afterExpiry.Should().NotBeNull();
        whileTakenOver.Should().BeNull("disposing the expired handle must not release the new holder");
    }

    [Test]
    public async Task Release_Uncontended_RemovesEntry()
    {
        // Arrange
        var handle = await _sut.TryAcquireLockAsync("job", Expiry);
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
        var act = async () => await _sut.TryAcquireLockAsync("job", Expiry, cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        _sut.HeldLockCount.Should().Be(0);
    }
}
