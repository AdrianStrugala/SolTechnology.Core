using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace SolTechnology.Core.Logging.Tests;

public sealed class TimingServiceTests
{
    [Test]
    public void StartContext_Single_Records_Elapsed()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sut = new TimingService(fakeTime);

        using (sut.StartContext("db"))
        {
            fakeTime.Advance(TimeSpan.FromMilliseconds(42));
        }

        var timings = sut.GetTimings();
        timings.Should().ContainKey("db");
        timings["db"].Should().Be(42);
    }

    [Test]
    public void StartContext_Multiple_Same_Name_Aggregates()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sut = new TimingService(fakeTime);

        using (sut.StartContext("http"))
        {
            fakeTime.Advance(TimeSpan.FromMilliseconds(10));
        }

        using (sut.StartContext("http"))
        {
            fakeTime.Advance(TimeSpan.FromMilliseconds(20));
        }

        sut.GetTimings()["http"].Should().Be(30);
    }

    [Test]
    public void StartContext_Different_Names_Tracked_Separately()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sut = new TimingService(fakeTime);

        using (sut.StartContext("db"))
        {
            fakeTime.Advance(TimeSpan.FromMilliseconds(5));
        }

        using (sut.StartContext("http"))
        {
            fakeTime.Advance(TimeSpan.FromMilliseconds(100));
        }

        var timings = sut.GetTimings();
        timings["db"].Should().Be(5);
        timings["http"].Should().Be(100);
    }

    [Test]
    public void Reset_Clears_All_Timings()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sut = new TimingService(fakeTime);

        using (sut.StartContext("x"))
        {
            fakeTime.Advance(TimeSpan.FromMilliseconds(1));
        }

        sut.Reset();
        sut.GetTimings().Should().BeEmpty();
    }

    [Test]
    public void GetTimings_Returns_Empty_When_No_Contexts_Used()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sut = new TimingService(fakeTime);

        sut.GetTimings().Should().BeEmpty();
    }

    [Test]
    public void Dispose_Is_Idempotent()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sut = new TimingService(fakeTime);

        var handle = sut.StartContext("x");
        fakeTime.Advance(TimeSpan.FromMilliseconds(10));
        handle.Dispose();

        fakeTime.Advance(TimeSpan.FromMilliseconds(999)); // should not be counted
        handle.Dispose(); // second dispose — no-op

        sut.GetTimings()["x"].Should().Be(10);
    }
}

