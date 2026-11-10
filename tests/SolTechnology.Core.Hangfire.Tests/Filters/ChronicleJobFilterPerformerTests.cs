using System.Collections.Concurrent;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Hangfire.Filters;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.Hangfire.Tests.Filters;

[TestFixture]
public class ChronicleJobFilterPerformerTests : ChronicleJobPerformerTestBase
{
    [Test]
    public void JobBody_ChronicleSteps_NestUnderJobRoot()
    {
        // Arrange
        var correlation = new CorrelationIdJobFilter(Services.GetRequiredService<ICorrelationIdService>(),
            Services.GetRequiredService<ILogger<CorrelationIdJobFilter>>());

        // Act
        var activeAfterJob = PerformProbe(new JobFilterCollection { Filter, correlation }, "LoadCities");

        // Assert
        activeAfterJob.Should().BeFalse();
        var report = Reports.Should().ContainSingle().Which;
        report.Message.Should().StartWith("Chronicle [Job ChronicleProbeJob.Run] [Ok]").And.Contain("\n`- LoadCities [@");
        report.Scopes.OfType<IEnumerable<KeyValuePair<string, object?>>>().SelectMany(s => s)
            .Should().Contain(p => p.Key == CorrelationId.ScopeKey);
    }

    [Test]
    public void SequentialJobsOnSameThread_SecondReportHasNoStepsOfFirst()
    {
        // Act
        var activeAfterJobs = OnWorkerThread(() =>
        {
            var afterFirst = PerformOnThisThread(new JobFilterCollection { Filter }, ProbeJob("FirstStep"));
            var afterSecond = PerformOnThisThread(new JobFilterCollection { Filter }, ProbeJob("SecondStep"));
            return (afterFirst, afterSecond);
        });

        // Assert
        activeAfterJobs.Should().Be((false, false));
        Reports.Should().HaveCount(2);
        Reports[1].Message.Should().Contain("SecondStep").And.NotContain("FirstStep");
    }

    [Test]
    public void OnPerforming_LaterFilterThrows_RootEmittedFailedAndAmbientRestored()
    {
        // Act
        var activeAfterJob = PerformProbe(new JobFilterCollection { Filter, new ThrowingFilter() }, "NeverRuns");

        // Assert
        activeAfterJob.Should().BeFalse();
        Reports.Should().ContainSingle().Which.Message.Should().StartWith("Chronicle [Job ChronicleProbeJob.Run] [Failed]")
            .And.Contain("Error=[InvalidOperationException: later filter failed]").And.NotContain("NeverRuns");
    }
}
