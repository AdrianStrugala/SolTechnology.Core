namespace SolTechnology.Core.Logging;

public interface ILoggableOperation
{
    public LogScope LogScope { get; }
}

public class LoggableOperation : ILoggableOperation
{
    public required LogScope LogScope { get; init; }
}