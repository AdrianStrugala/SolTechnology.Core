using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.Cache.Tests;

[TestFixture]
public class ScopedCacheTests
{
    private readonly ScopedCache<string, int> _sut = new();

    [Test]
    public async Task GetOrAdd_ScopedFactoryFaults_NextCallRetries()
    {
        // Arrange
        var calls = 0;
        var first = async () => await _sut.GetOrAdd("key", async _ =>
        {
            calls++;
            await Task.Yield();
            throw new InvalidOperationException("boom");
        });
        await first.Should().ThrowAsync<InvalidOperationException>();

        // Act
        var second = await _sut.GetOrAdd("key", _ => { calls++; return Task.FromResult(42); });

        // Assert
        second.Should().Be(42);
        calls.Should().Be(2);
    }
}
