using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.Redis.Testing;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache.Tests.Redis;

[SetUpFixture]
public static class RedisTestEnvironment
{
    public static RedisFixture Redis { get; private set; } = null!;

    public static ConnectionMultiplexer RawConnection { get; private set; } = null!;

    [OneTimeSetUp]
    public static async Task SetUp()
    {
        Redis = new RedisFixture(containerName: "soltech-cache-tests-redis");
        await Redis.InitializeAsync();
        RawConnection = await ConnectionMultiplexer.ConnectAsync(Redis.ConnectionString);
    }

    [OneTimeTearDown]
    public static async Task TearDown()
    {
        await RawConnection.DisposeAsync();
        await Redis.DisposeAsync();
    }

    public static ServiceProvider BuildProvider(string instanceName)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddSolDistributedCache(new DistributedCacheConfiguration
        {
            ConnectionString = Redis.ConnectionString,
            InstanceName = instanceName
        });
        services.AddSolDistributedLock();
        services.AddSolDistributedIdempotency();
        return services.BuildServiceProvider();
    }

    public static IReadOnlyList<string> RawKeys()
    {
        var server = RawConnection.GetServer(RawConnection.GetEndPoints().Single());
        return server.Keys(pattern: "*").Select(key => key.ToString()).OrderBy(key => key).ToList();
    }
}
