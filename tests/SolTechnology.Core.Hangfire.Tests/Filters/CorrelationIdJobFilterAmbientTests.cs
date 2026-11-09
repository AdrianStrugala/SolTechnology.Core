using FluentAssertions;
using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Hangfire.Filters;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.Hangfire.Tests.Filters;

/// <summary>Runs the filter against the real AsyncLocal-backed correlation service.</summary>
[TestFixture]
public class CorrelationIdJobFilterAmbientTests
{
    private ICorrelationIdService _correlationIdService = null!;
    private CorrelationIdJobFilter _sut = null!;

    [SetUp]
    public void Setup()
    {
        _correlationIdService = new ServiceCollection().AddSolCorrelationIdService()
            .BuildServiceProvider().GetRequiredService<ICorrelationIdService>();
        _sut = new CorrelationIdJobFilter(_correlationIdService, NullLogger<CorrelationIdJobFilter>.Instance);
    }

    [Test]
    public void OnCreating_TwoJobsSameThread_GetDistinctIds()
    {
        OnFreshExecutionContext(() =>
        {
            // Arrange
            var first = CreateCreatingContext();
            var second = CreateCreatingContext();

            // Act
            _sut.OnCreating(first);
            _sut.OnCreating(second);

            // Assert
            first.GetJobParameter<string>(CorrelationId.ScopeKey)
                .Should().NotBe(second.GetJobParameter<string>(CorrelationId.ScopeKey));
            _correlationIdService.Get().Should().BeNull();
        });
    }

    [TestCase("outer-id")]
    [TestCase(null)]
    public void OnPerformed_RestoresPreviousAmbientId(string? previousId)
    {
        OnFreshExecutionContext(() =>
        {
            // Arrange
            if (previousId is not null)
            {
                _correlationIdService.Set(CorrelationId.FromString(previousId));
            }

            var performing = CreatePerformingContext("job-id");
            _sut.OnPerforming(performing);
            var duringJob = _correlationIdService.Get()?.Value;

            // Act
            _sut.OnPerformed(new PerformedContext(performing, null, false, null));
            var afterJob = _correlationIdService.Get()?.Value;

            // Assert
            duringJob.Should().Be("job-id");
            afterJob.Should().Be(previousId);
        });
    }

    private static void OnFreshExecutionContext(Action test)
    {
        using (ExecutionContext.SuppressFlow())
        {
            Task.Run(test).GetAwaiter().GetResult();
        }
    }

    private static CreatingContext CreateCreatingContext() =>
        new(new CreateContext(Substitute.For<JobStorage>(), Substitute.For<IStorageConnection>(),
            Job.FromExpression(() => Console.WriteLine("test")), null));

    private static PerformingContext CreatePerformingContext(string storedCorrelationId)
    {
        var connection = Substitute.For<IStorageConnection>();
        connection.GetJobParameter("job-1", CorrelationId.ScopeKey).Returns($"\"{storedCorrelationId}\"");
        var backgroundJob = new BackgroundJob("job-1", Job.FromExpression(() => Console.WriteLine("test")), DateTime.UtcNow);
        return new PerformingContext(new PerformContext(Substitute.For<JobStorage>(), connection, backgroundJob, Substitute.For<IJobCancellationToken>()));
    }
}
