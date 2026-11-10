using FluentAssertions;
using Hangfire.Common;
using Hangfire.Server;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Hangfire.Filters;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.Hangfire.Tests.Filters;

[TestFixture]
public class ChronicleJobFilterRootTests : ChronicleJobFilterTestBase
{
    [TestCase("recurring", "Job TestJob")]
    [TestCase("event", "Event ProbeEvent")]
    [TestCase("legacy event", "Event ProbeEvent")]
    [TestCase("other", "Job Console.WriteLine")]
    public void OnPerforming_JobKind_RootNamedAfterTheWork(string kind, string expectedName)
    {
        // Arrange
        var job = kind switch
        {
            "recurring" => RunnerJob<TestJob>(),
            "event" => Job.FromExpression<HangfireEventPublisher>(p => p.DispatchInScope(new ProbeEvent(), CancellationToken.None)),
            "legacy event" => Job.FromExpression<HangfireEventPublisher>(p => p.DispatchInScope(new ProbeEvent())),
            _ => Job.FromExpression(() => Console.WriteLine("test"))
        };

        // Act
        Perform(job);

        // Assert
        Reports.Should().ContainSingle().Which.Message.Should().StartWith($"Chronicle [{expectedName}] [Ok]");
    }

    [Test]
    public void OnPerforming_InsideOpenRoot_StartsSeparateRootAndRestoresIt()
    {
        // Act
        var (insideJob, afterJob) = OnWorkerThread(() =>
        {
            var outer = Chronicle.Begin("Outer");
            var performing = new PerformingContext(PerformContext(RunnerJob<TestJob>()));
            Filter.OnPerforming(performing);
            var inside = Chronicle.Current.IsRecording && Reports.Count == 0;
            Filter.OnPerformed(new PerformedContext(performing, null, false, null));
            using (Chronicle.Step("AfterJob"))
            {
            }

            outer.Dispose();
            return (inside, Chronicle.IsActive);
        });

        // Assert
        insideJob.Should().BeTrue();
        afterJob.Should().BeFalse();
        Reports.Select(r => r.Message[..(r.Message.IndexOf(']') + 1)]).Should().Equal("Chronicle [Job TestJob]", "Chronicle [Outer]");
        Reports[0].Message.Should().Contain("DetachedFrom=Outer");
        Reports[1].Message.Should().Contain("`- AfterJob");
    }

    [Test]
    public void ChronicleJobFilter_Order_IsZeroAndRunsAfterCorrelationAndOverlapFilters()
    {
        // Arrange
        var correlation = new CorrelationIdJobFilter(Substitute.For<ICorrelationIdService>(), NullLogger<CorrelationIdJobFilter>.Instance);
        var filters = new JobFilterCollection { Filter, correlation, new SmartRetryJobFilter() };
        var overlap = new PreventOverlapFilterProvider([new RecurringJobDescriptor(typeof(TestJob), "* * * * *", preventOverlap: true)]);

        // Act
        var ordered = new JobFilterProviderCollection(filters, overlap).GetFilters(RunnerJob<TestJob>()).ToList();

        // Assert
        Filter.Order.Should().Be(0);
        ordered.Select(f => f.Instance.GetType().Name).Should().Equal(
            nameof(CorrelationIdJobFilter), nameof(SmartRetryJobFilter), nameof(PreventOverlapJobFilter), nameof(ChronicleJobFilter));
    }
}
