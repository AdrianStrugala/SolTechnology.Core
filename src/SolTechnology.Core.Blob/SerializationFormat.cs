namespace SolTechnology.Core.Blob;

/// <summary>
/// The format <see cref="BlobContainerClientExtensions"/> use to serialize blob content; it is stored in the blob's
/// <c>SerializationFormat</c> metadata key.
/// </summary>
public enum SerializationFormat
{
    /// <summary>
    /// UTF-8 JSON through <c>System.Text.Json</c>; the default, and the format assumed when the metadata key is missing.
    /// </summary>
    Json = 0,

    /// <summary>
    /// Avro with the Brotli codec through AvroConvert; smaller blobs at the cost of readability.
    /// </summary>
    Avro = 1
}