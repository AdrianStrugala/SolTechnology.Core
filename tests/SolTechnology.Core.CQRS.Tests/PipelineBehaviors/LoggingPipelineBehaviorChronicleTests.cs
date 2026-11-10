using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Operations;

namespace SolTechnology.Core.CQRS.Tests.PipelineBehaviors;

[TestFixture]
public class LoggingPipelineBehaviorChronicleTests : LoggingPipelineBehaviorChronicleTestBase
{
    [Test]
    public async Task Handle_OutsideRoot_EmitsOne2154EntryNamedAfterRequest()
    {
        // Act
        await Sut<ProbeCommand, Result>().Handle(new ProbeCommand("Warsaw"), () => Result.SuccessAsTask(), CancellationToken.None);

        // Assert
        var report = Host.Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Information);
        report.Message.Should().StartWith("Chronicle [ProbeCommand] [Ok] in [").And.Contain("\nProbeCommand [@0 ms]");
    }

    [Test]
    public async Task Handle_InsideRoot_AddsChildStepNamedAfterRequest()
    {
        // Act
        using (Host.Chronicle.Begin("ImportCities"))
        {
            await Sut<ProbeCommand, Result>().Handle(new ProbeCommand("Warsaw"), () => Result.SuccessAsTask(), CancellationToken.None);
        }

        // Assert
        Host.Reports.Should().ContainSingle().Which.Message.Should()
            .StartWith("Chronicle [ImportCities] [Ok]").And.Contain("\n`- ProbeCommand [@");
    }

    [Test]
    public async Task Handle_NestedSend_BuildsNestedSteps()
    {
        // Arrange
        var inner = Sut<InnerCommand, Result>();

        // Act
        await Sut<ProbeCommand, Result>().Handle(new ProbeCommand("Warsaw"),
            () => inner.Handle(new InnerCommand("Cracow"), () => Result.SuccessAsTask(), CancellationToken.None),
            CancellationToken.None);

        // Assert
        Host.Reports.Should().ContainSingle().Which.Message.Should().Contain("\nProbeCommand [@0 ms]").And.Contain("\n`- InnerCommand [@");
    }

    [Test]
    public async Task Handle_MaskedLogScopeProperty_AttributeHoldsMaskedValueOnly()
    {
        // Act
        await Sut<ScopedCommand, Result>().Handle(new ScopedCommand("Warsaw", "jan.kowalski@example.com"),
            () => Result.SuccessAsTask(), CancellationToken.None);

        // Assert
        Host.Reports.Should().ContainSingle().Which.Message.Should()
            .Contain($"CityName=Warsaw UserEmail={LoggingDefaults.MaskedValue}").And.NotContain("jan.kowalski");
    }

    [Test]
    public async Task Handle_WithOperationsListener_StartsExactlyOneActivityWithTypedScopeTags()
    {
        // Arrange
        var started = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CoreLoggingActivitySources.OperationsName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = started.Enqueue
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        await Sut<TypedScopeCommand, Result>().Handle(new TypedScopeCommand(42), () => Result.SuccessAsTask(), CancellationToken.None);

        // Assert
        var activity = started.Should().ContainSingle(a => a.OperationName == nameof(TypedScopeCommand)).Subject;
        activity.Source.Name.Should().Be(CoreLoggingActivitySources.OperationsName);
        activity.GetTagItem("Limit").Should().Be(42);
    }

    [TestCase(false, new[] { 2137, 2138 })]
    [TestCase(true, new[] { 2137, 2139 })]
    public async Task Handle_LifecycleEntries2137To2139_Unchanged(bool throws, int[] expectedEventIds)
    {
        // Act
        try
        {
            await Sut<ProbeCommand, Result>().Handle(new ProbeCommand("Warsaw"),
                () => throws ? throw new InvalidOperationException("boom") : Result.SuccessAsTask(), CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
        }

        // Assert
        Host.Logs.GetSnapshot().Where(r => r.Category!.Contains("LoggingPipelineBehavior"))
            .Select(r => r.Id.Id).Should().Equal(expectedEventIds);
    }
}
