using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace SolTechnology.Core.Cache.Tests;

[TestFixture]
public class SingletonCacheTests
{
    private SingletonCache _sut = null!;
    private MemoryCache _memoryCache = null!;

    [SetUp]
    public void SetUp()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var configuration = Substitute.For<IOptionsMonitor<CacheConfiguration>>();
        configuration.CurrentValue.Returns(new CacheConfiguration
        {
            ExpirationMode = ExpirationMode.Absolute,
            ExpirationSeconds = 60
        });
        _sut = new SingletonCache(_memoryCache, configuration);
    }

    [TearDown]
    public void TearDown() => _memoryCache.Dispose();

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetOrAdd_FactoryFaults_NextCallRetriesFactory(bool throwSynchronously)
    {
        // Arrange
        var calls = 0;
        Func<string, Task<int>> failing = throwSynchronously
            ? _ => { calls++; throw new InvalidOperationException("boom"); }
            : async _ => { calls++; await Task.Yield(); throw new InvalidOperationException("boom"); };
        var first = async () => await _sut.GetOrAdd("key", failing);
        await first.Should().ThrowAsync<InvalidOperationException>();

        // Act
        var second = await _sut.GetOrAdd("key", _ => { calls++; return Task.FromResult(42); });

        // Assert
        second.Should().Be(42);
        calls.Should().Be(2);
    }

    [Test]
    public async Task GetOrAdd_ConcurrentMisses_FactoryRunsOnce()
    {
        // Arrange
        var calls = 0;
        using var start = new ManualResetEventSlim();
        var callers = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() =>
            {
                start.Wait();
                return _sut.GetOrAdd("key", async _ =>
                {
                    Interlocked.Increment(ref calls);
                    await Task.Delay(50);
                    return 42;
                });
            }))
            .ToArray();

        // Act
        start.Set();
        var results = await Task.WhenAll(callers);

        // Assert
        results.Should().AllSatisfy(result => result.Should().Be(42));
        calls.Should().Be(1);
    }
}
