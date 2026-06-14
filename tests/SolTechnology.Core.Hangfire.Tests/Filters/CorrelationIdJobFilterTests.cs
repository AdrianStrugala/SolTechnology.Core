using FluentAssertions;
using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Hangfire.Filters;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.Hangfire.Tests.Filters;

[TestFixture]
public class CorrelationIdJobFilterTests
{
    private ICorrelationIdService _correlationIdService = null!;
    private CorrelationIdJobFilter _sut = null!;

    [SetUp]
    public void Setup()
    {
        _correlationIdService = Substitute.For<ICorrelationIdService>();
        _sut = new CorrelationIdJobFilter(_correlationIdService, NullLogger<CorrelationIdJobFilter>.Instance);
    }

    [Test]
    public void OnCreating_SavesCurrentCorrelationIdAsJobParameter()
    {
        // Arrange
        var correlationId = CorrelationId.FromString("test-correlation-123");
        _correlationIdService.GetOrGenerate().Returns(correlationId);

        var context = CreateCreatingContext();

        // Act
        _sut.OnCreating(context);

        // Assert
        var stored = context.GetJobParameter<string>(CorrelationId.ScopeKey);
        stored.Should().Be("test-correlation-123");
    }

    [Test]
    public void OnPerforming_RestoresCorrelationIdFromJobParameter()
    {
        // Arrange
        var connection = Substitute.For<IStorageConnection>();
        // Hangfire serializes string parameters as plain JSON strings (quoted)
        connection.GetJobParameter("job-1", CorrelationId.ScopeKey)
            .Returns("\"restored-id-456\"");

        var context = CreatePerformingContext(connection);

        // Act
        _sut.OnPerforming(context);

        // Assert
        _correlationIdService.Received(1).Set(Arg.Is<CorrelationId>(c => c.Value == "restored-id-456"));
    }

    [Test]
    public void OnPerforming_WhenNoStoredCorrelation_GeneratesNew()
    {
        // Arrange
        var generated = CorrelationId.FromString("generated-789");
        _correlationIdService.GetOrGenerate().Returns(generated);

        var context = CreatePerformingContext();

        // Act
        _sut.OnPerforming(context);

        // Assert
        _correlationIdService.Received(1).GetOrGenerate();
    }

    [Test]
    public void OnPerformed_DisposesLogScope()
    {
        // Arrange
        var scope = Substitute.For<IDisposable>();

        var performingContext = CreatePerformingContext();
        performingContext.Items["CorrelationIdLogScope"] = scope;

        var performedContext = CreatePerformedContext(performingContext);

        // Act
        _sut.OnPerformed(performedContext);

        // Assert
        scope.Received(1).Dispose();
    }

    [Test]
    public void OnPerformed_WhenNoScopeInItems_DoesNotThrow()
    {
        // Arrange
        var performingContext = CreatePerformingContext();
        var performedContext = CreatePerformedContext(performingContext);

        // Act & Assert
        var act = () => _sut.OnPerformed(performedContext);
        act.Should().NotThrow();
    }

    private static CreatingContext CreateCreatingContext()
    {
        var storage = Substitute.For<JobStorage>();
        var connection = Substitute.For<IStorageConnection>();
        var job = Job.FromExpression(() => Console.WriteLine("test"));
        return new CreatingContext(new CreateContext(storage, connection, job, null));
    }

    private static PerformingContext CreatePerformingContext(IStorageConnection? connection = null)
    {
        var storage = Substitute.For<JobStorage>();
        connection ??= Substitute.For<IStorageConnection>();
        var job = Job.FromExpression(() => Console.WriteLine("test"));
        var backgroundJob = new BackgroundJob("job-1", job, DateTime.UtcNow);
        return new PerformingContext(new PerformContext(storage, connection, backgroundJob, Substitute.For<IJobCancellationToken>()));
    }

    private static PerformedContext CreatePerformedContext(PerformingContext performing)
    {
        return new PerformedContext(performing, null, false, null);
    }
}

