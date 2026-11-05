using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SolTechnology.Avro;

namespace SolTechnology.Core.Blob
{
    public static class BlobContainerClientExtensions
    {
        private const string SerializationFormatKey = "SerializationFormat";

        public static async Task<T> ReadFromBlob<T>(
            this BlobContainerClient client,
            string blobName,
            CancellationToken ct = default)
        {
            var blob = client.GetBlobClient(blobName);
            var content = (await blob.DownloadContentAsync(ct)).Value;

            string? contentType = null;
            content.Details.Metadata?.TryGetValue(SerializationFormatKey, out contentType);

            switch (contentType)
            {
                case "avro":
                    return AvroConvert.Deserialize<T>(content.Content.ToArray());

                case "json":
                default:
                    return content.Content.ToObjectFromJson<T>()!;
            }
        }

        public static async Task WriteToBlob(
            this BlobContainerClient client,
            string blobName,
            object content,
            CancellationToken ct = default)
        {
            await WriteToBlob(client, blobName, content, SerializationFormat.Json, ct);
        }

        public static async Task WriteToBlob(
            this BlobContainerClient client,
            string blobName,
            object content,
            SerializationFormat serializationFormat,
            CancellationToken ct = default)
        {
            var blob = client.GetBlobClient(blobName);
            var serializedContent = Serialize(content, serializationFormat);
            var options = new BlobUploadOptions
            {
                Metadata = await MergeFormatIntoMetadata(blob, serializationFormat, ct)
            };

            try
            {
                await blob.UploadAsync(serializedContent, options, ct);
            }
            catch (RequestFailedException e) when (e.ErrorCode == BlobErrorCode.ContainerNotFound)
            {
                await client.CreateIfNotExistsAsync(cancellationToken: ct);
                await blob.UploadAsync(serializedContent, options, ct);
            }
        }

        private static BinaryData Serialize(object content, SerializationFormat serializationFormat)
        {
            switch (serializationFormat)
            {
                case SerializationFormat.Json:
                    return BinaryData.FromObjectAsJson(content);

                case SerializationFormat.Avro:
                    return new BinaryData(AvroConvert.Serialize(content, CodecType.Brotli));

                default:
                    return BinaryData.Empty;
            }
        }

        // An overwriting upload replaces all metadata, so keep the blob's existing keys (RM-12).
        private static async Task<Dictionary<string, string>> MergeFormatIntoMetadata(
            BlobClient blob,
            SerializationFormat serializationFormat,
            CancellationToken ct)
        {
            var metadata = (await blob.ExistsAsync(ct)).Value
                ? new Dictionary<string, string>((await blob.GetPropertiesAsync(cancellationToken: ct)).Value.Metadata)
                : new Dictionary<string, string>();

            metadata[SerializationFormatKey] = serializationFormat.ToString().ToLowerInvariant();
            return metadata;
        }
    }
}
