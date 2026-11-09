using System.Reflection;
using FluentAssertions;
using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Hangfire.Filters;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.Hangfire.Tests;

/// <summary>
/// Hangfire stores these names in every enqueued, scheduled, and recurring job; changing one breaks
/// jobs already in storage (premortem RM-7, RM-16).
/// </summary>
[TestFixture]
public class HangfirePersistedIdentityTests
{
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    [Test]
    public void HangfireEventPublisher_KeepsPersistedIdentity()
    {
        // Arrange
        var publisher = typeof(HangfireEventPublisher);

        // Act
        var legacyOverload = publisher.GetMethod("DispatchInScope", PublicInstance, [typeof(IEvent)]);
        var cancellableOverload = publisher.GetMethod("DispatchInScope", PublicInstance, [typeof(IEvent), typeof(CancellationToken)]);

        // Assert
        publisher.FullName.Should().Be("SolTechnology.Core.Hangfire.HangfireEventPublisher");
        publisher.Assembly.GetName().Name.Should().Be("SolTechnology.Core.Hangfire");
        legacyOverload.Should().NotBeNull();
        cancellableOverload.Should().NotBeNull();
    }

    [Test]
    public void RecurringJobRunner_KeepsPersistedIdentity()
    {
        // Arrange
        var runner = typeof(RecurringJobRunner<>);

        // Act
        var runAsync = runner.GetMethod("RunAsync", PublicInstance, [typeof(CancellationToken)]);

        // Assert
        runner.FullName.Should().Be("SolTechnology.Core.Hangfire.RecurringJobRunner`1");
        runner.Assembly.GetName().Name.Should().Be("SolTechnology.Core.Hangfire");
        runAsync.Should().NotBeNull();
        runAsync!.ReturnType.Should().Be<Task<Result>>();
    }

    [Test]
    public void CorrelationIdJobFilter_KeepsCorrelationJobParameterKey()
    {
        // Arrange
        var sut = new CorrelationIdJobFilter(Substitute.For<ICorrelationIdService>(), NullLogger<CorrelationIdJobFilter>.Instance);
        var context = new CreatingContext(new CreateContext(Substitute.For<JobStorage>(), Substitute.For<IStorageConnection>(),
            Job.FromExpression(() => Console.WriteLine("test")), null));

        // Act
        sut.OnCreating(context);

        // Assert
        context.Parameters.Keys.Should().Equal("CorrelationId");
    }
}
