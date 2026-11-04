using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace SolTechnology.Core.Cache.Tests.Redis;

[TestFixture]
public class RedisKeysTests
{
    [SetUp]
    public async Task SetUp() => await RedisTestEnvironment.Redis.FlushAsync();

    [Test]
    public async Task RedisKeys_LockAndIdempotency_KeepInstanceNamePrefixFormat()
    {
        // Arrange
        await using var provider = RedisTestEnvironment.BuildProvider("SolTechnology:");
        var lockService = provider.GetRequiredService<IDistributedLockService>();
        var idempotencyStore = provider.GetRequiredService<IIdempotencyStore>();

        // Act
        var handle = await lockService.TryAcquireLock("nightly-job", TimeSpan.FromMinutes(1));
        var reserved = await idempotencyStore.TryAdd("request-1");
        var rawKeys = RedisTestEnvironment.RawKeys();

        // Assert
        handle.Should().NotBeNull();
        reserved.Should().BeTrue();
        rawKeys.Should().BeEquivalentTo("SolTechnology:idempotency:request-1", "SolTechnology:lock:nightly-job");
    }
}
