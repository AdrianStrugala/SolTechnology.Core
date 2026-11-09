using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace SolTechnology.Core.Cache.Tests.Redis;

[TestFixture]
public class RedisCacheTests
{
    private ServiceProvider _provider = null!;
    private IRedisCache _sut = null!;

    public sealed record SomeTestDto(string Name, int Population);

    public sealed record OtherTestDto(string Name);

    [SetUp]
    public async Task SetUp()
    {
        await RedisTestEnvironment.Redis.Flush();
        _provider = RedisTestEnvironment.BuildProvider("app:");
        _sut = _provider.GetRequiredService<IRedisCache>();
    }

    [TearDown]
    public async Task TearDown() => await _provider.DisposeAsync();

    [Test]
    public async Task GetOrAdd_SameKeyDifferentItemType_DoesNotCollide()
    {
        // Arrange
        await _sut.GetOrAdd(42, _ => Task.FromResult(new SomeTestDto("Wroclaw", 640_000)));

        // Act
        var other = await _sut.GetOrAdd(42, _ => Task.FromResult(new OtherTestDto("from factory")));
        var cached = await _sut.GetOrAdd(42, _ => Task.FromResult(new SomeTestDto("from factory", 0)));

        // Assert
        other.Should().Be(new OtherTestDto("from factory"));
        cached.Should().Be(new SomeTestDto("Wroclaw", 640_000));
    }

    [Test]
    public async Task CacheKey_GenericItemType_HasNoAssemblyVersion()
    {
        // Act
        await _sut.GetOrAdd("wroclaw", _ => Task.FromResult(new List<SomeTestDto> { new("Wroclaw", 640_000) }));
        var rawKeys = RedisTestEnvironment.RawKeys();

        // Assert
        var rawKey = rawKeys.Should().ContainSingle().Subject;
        rawKey.Should().Be(
            "app:System.String:System.Collections.Generic.List`1[SolTechnology.Core.Cache.Tests.Redis.RedisCacheTests+SomeTestDto]:\"wroclaw\"");
        rawKey.Should().NotContainAny("Version=", "Culture=", "PublicKeyToken=");
    }
}
