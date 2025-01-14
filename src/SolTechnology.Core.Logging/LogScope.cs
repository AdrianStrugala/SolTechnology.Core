using System.Text.Json.Serialization;

namespace SolTechnology.Core.Logging;

public class LogScope
{
    [JsonIgnore]
    public required object OperationId { get; init; }

    [JsonIgnore]
    public required string OperationIdName { get; init; }

    [JsonIgnore]
    public required string OperationName { get; init; }
}