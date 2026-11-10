using SolTechnology.Core.CQRS.PipelineBehaviors;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Masking;

namespace SolTechnology.Core.CQRS.Tests.PipelineBehaviors;

/// <summary>Runs <see cref="LoggingPipelineBehavior{TRequest,TResponse}"/> against a real Chronicle.</summary>
public abstract class LoggingPipelineBehaviorChronicleTestBase
{
    protected ChronicleTestHost Host { get; private set; } = null!;

    public sealed record ProbeCommand(string Name);

    public sealed record InnerCommand(string Name);

    public sealed record ScopedCommand(
        [property: LogScope] string CityName,
        [property: LogScope("UserEmail"), Masked] string Email);

    [SetUp]
    public void SetUpHost() => Host = new ChronicleTestHost();

    [TearDown]
    public void TearDownHost() => Host.Dispose();

    protected LoggingPipelineBehavior<TRequest, TResponse> Sut<TRequest, TResponse>() where TRequest : notnull
        => new(Host.Logger<LoggingPipelineBehavior<TRequest, TResponse>>(), Host.Chronicle);
}
