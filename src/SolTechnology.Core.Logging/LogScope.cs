namespace SolTechnology.Core.Logging;

public class LogScope
{
    public required object OperationId { get; set; }
    public required string OperationIdName { get; set; }
    public required string OperationName { get; set; }
}