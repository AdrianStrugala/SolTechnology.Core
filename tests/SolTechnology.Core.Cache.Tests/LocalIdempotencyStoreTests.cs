using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace SolTechnology.Core.Cache.Tests;

/// <summary>
/// Unit tests for <see cref="LocalIdempotencyStore"/> — reservation, replay, removal, and
/// TTL expiry (driven deterministically by <see cref="FakeTimeProvider"/>).
/// </summary>
public sealed class LocalIdempotencyStoreTests
{
    private static StoredResponse SampleResponse() => new()
    {
        StatusCode = 201,
        Headers = new Dictionary<string, string[]> { ["Location"] = ["/things/1"] },
        Body = "created"u8.ToArray()
    };

    [Test]
    public async Task TryAdd_FirstCall_Wins()
    {
        var sut = new LocalIdempotencyStore(TimeSpan.FromHours(1), new FakeTimeProvider());

        var first = await sut.TryAdd("key-1");

        first.Should().BeTrue();
    }

    [Test]
    public async Task TryAdd_SecondCall_SameKey_Loses()
    {
        var sut = new LocalIdempotencyStore(TimeSpan.FromHours(1), new FakeTimeProvider());

        await sut.TryAdd("key-1");
        var second = await sut.TryAdd("key-1");

        second.Should().BeFalse();
    }

    [Test]
    public async Task Get_AfterReserveButNoResponse_ReturnsNull()
    {
        var sut = new LocalIdempotencyStore(TimeSpan.FromHours(1), new FakeTimeProvider());

        await sut.TryAdd("key-1");
        var result = await sut.Get("key-1");

        result.Should().BeNull();
    }

    [Test]
    public async Task SetResponse_ThenGet_ReturnsStoredResponse()
    {
        var sut = new LocalIdempotencyStore(TimeSpan.FromHours(1), new FakeTimeProvider());
        var response = SampleResponse();

        await sut.TryAdd("key-1");
        await sut.SetResponse("key-1", response);
        var result = await sut.Get("key-1");

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(201);
        result.Body.Should().Equal(response.Body);
    }

    [Test]
    public async Task Remove_AllowsReReservation()
    {
        var sut = new LocalIdempotencyStore(TimeSpan.FromHours(1), new FakeTimeProvider());

        await sut.TryAdd("key-1");
        await sut.Remove("key-1");
        var reReserve = await sut.TryAdd("key-1");

        reReserve.Should().BeTrue();
    }

    [Test]
    public async Task Get_AfterTtlExpiry_ReturnsNull()
    {
        var time = new FakeTimeProvider();
        var sut = new LocalIdempotencyStore(TimeSpan.FromMinutes(10), time);

        await sut.TryAdd("key-1");
        await sut.SetResponse("key-1", SampleResponse());

        time.Advance(TimeSpan.FromMinutes(11)); // past TTL

        var result = await sut.Get("key-1");
        result.Should().BeNull();
    }

    [Test]
    public async Task TryAdd_AfterExpiry_AllowsReReservation()
    {
        var time = new FakeTimeProvider();
        var sut = new LocalIdempotencyStore(TimeSpan.FromMinutes(10), time);

        await sut.TryAdd("key-1");
        time.Advance(TimeSpan.FromMinutes(11)); // expired — eviction on next write

        var reReserve = await sut.TryAdd("key-1");
        reReserve.Should().BeTrue();
    }

    [Test]
    public async Task Get_BeforeTtlExpiry_StillReturnsResponse()
    {
        var time = new FakeTimeProvider();
        var sut = new LocalIdempotencyStore(TimeSpan.FromMinutes(10), time);

        await sut.TryAdd("key-1");
        await sut.SetResponse("key-1", SampleResponse());

        time.Advance(TimeSpan.FromMinutes(9)); // still within TTL

        var result = await sut.Get("key-1");
        result.Should().NotBeNull();
    }
}

