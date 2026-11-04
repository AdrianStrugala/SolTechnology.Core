using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.Cache.HealthChecks;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache.Tests;

[TestFixture]
public class ModuleResolutionTests
{
    private const string UnreachableRedis = "localhost:1,connectTimeout=100";

    private static readonly ServiceProviderOptions StrictValidation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    [Test]
    public async Task AddSolLocalCache_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSolLocalCache();
        services.AddSolLocalLock();
        services.AddSolLocalIdempotency();
        await using var provider = services.BuildServiceProvider(StrictValidation);
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;

        // Act
        var singletonCache = scoped.GetRequiredService<ISingletonCache>();
        var scopedCache = scoped.GetRequiredService<IScopedCache<string, int>>();
        var lockService = scoped.GetRequiredService<IDistributedLockService>();
        var idempotencyStore = scoped.GetRequiredService<IIdempotencyStore>();
        var options = scoped.GetRequiredService<IOptions<CacheConfiguration>>().Value;

        // Assert
        singletonCache.Should().BeOfType<SingletonCache>();
        scopedCache.Should().BeOfType<ScopedCache<string, int>>();
        lockService.Should().BeOfType<LocalDistributedLockService>();
        idempotencyStore.Should().BeOfType<LocalIdempotencyStore>();
        options.ExpirationSeconds.Should().BePositive();
    }

    [Test]
    public async Task AddSolDistributedCache_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSolDistributedCache(new DistributedCacheConfiguration
        {
            ConnectionString = UnreachableRedis,
            InstanceName = "resolution-test:"
        });
        services.AddSolDistributedLock();
        services.AddSolDistributedIdempotency();
        services.AddHealthChecks().AddSolRedisHealthCheck();
        await using var provider = services.BuildServiceProvider(StrictValidation);
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;

        // Act
        var redisCache = scoped.GetRequiredService<IRedisCache>();
        var distributedCache = scoped.GetRequiredService<IDistributedCache>();
        var multiplexer = scoped.GetRequiredService<IConnectionMultiplexer>();
        var lockService = scoped.GetRequiredService<IDistributedLockService>();
        var idempotencyStore = scoped.GetRequiredService<IIdempotencyStore>();
        var healthCheck = scoped.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations
            .Single(registration => registration.Name == "redis")
            .Factory(scoped);

        // Assert
        redisCache.Should().BeOfType<RedisCache>();
        distributedCache.Should().NotBeNull();
        multiplexer.Should().NotBeNull();
        lockService.Should().BeOfType<RedisDistributedLockService>();
        idempotencyStore.Should().BeOfType<RedisIdempotencyStore>();
        healthCheck.Should().BeOfType<RedisHealthCheck>();
    }
}
