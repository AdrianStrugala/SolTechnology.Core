using System.Threading;
using FluentAssertions;
using Xunit;

namespace SolTechnology.Core.Logging.Tests;

public class ValueStopwatchTests
{
    [Fact]
    public void StartNew_returns_running_stopwatch()
    {
        var sw = ValueStopwatch.StartNew();
        Thread.Sleep(20);

        sw.Elapsed.Should().BeGreaterThan(TimeSpan.Zero);
        sw.ElapsedMilliseconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Two_independent_stopwatches_in_same_async_flow_do_not_share_state()
    {
        // Regression: replacement of the previous AsyncLocal-backed AsyncStopwatch.
        var outer = ValueStopwatch.StartNew();
        await Task.Delay(30);

        var inner = ValueStopwatch.StartNew();
        await Task.Delay(30);

        outer.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(inner.ElapsedMilliseconds);
    }
}

