namespace SolTechnology.Core.HTTP;

/// <summary>
/// Wire format of a request or response body handled by <see cref="RequestBuilder"/>.
/// </summary>
public enum DataType
{
    /// <summary>JSON through <c>System.Text.Json</c> (<c>application/json</c>).</summary>
    Json = 0,

    /// <summary>Avro through AvroConvert (<c>application/avro</c>).</summary>
    Avro = 1
}