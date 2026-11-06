using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SolTechnology.Avro;

namespace SolTechnology.Core.Blob
{
    /// <summary>
    /// Reads and writes typed objects as blobs, recording the <see cref="SerializationFormat"/> in the blob's metadata.
    /// </summary>
    public static class BlobContainerClientExtensions
    {
        private const string SerializationFormatKey = "SerializationFormat";

        // Classic form: an extension block drops T's nullable annotation, so callers get a notnull constraint (CS8714).
        /// <summary>
        /// Downloads the blob and deserializes it in the format stored in its metadata; a blob without the format key
        /// is read as JSON.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="client">The container that holds the blob.</param>
        /// <param name="blobName">The blob name.</param>
        /// <param name="ct">Cancels the download.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="RequestFailedException">The blob or the container does not exist, or the request failed.</exception>
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

        /// <param name="client">The container to write to.</param>
        extension(BlobContainerClient client)
        {
            /// <summary>
            /// Serializes <paramref name="content"/> as JSON and uploads it, overwriting an existing blob.
            /// </summary>
            /// <param name="blobName">The blob name.</param>
            /// <param name="content">The object to serialize.</param>
            /// <param name="ct">Cancels the upload.</param>
            public async Task WriteToBlob(
                string blobName,
                object content,
                CancellationToken ct = default)
            {
                await client.WriteToBlob(blobName, content, SerializationFormat.Json, ct);
            }

            /// <summary>
            /// Serializes <paramref name="content"/> in <paramref name="serializationFormat"/> and uploads it, overwriting an
            /// existing blob. The format key travels with the content in one upload, and the blob's other metadata keys are
            /// kept; a container deleted since it was created is created again once.
            /// </summary>
            /// <param name="blobName">The blob name.</param>
            /// <param name="content">The object to serialize.</param>
            /// <param name="serializationFormat">The format to serialize in.</param>
            /// <param name="ct">Cancels the upload.</param>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="serializationFormat"/> is not a defined <see cref="SerializationFormat"/>; nothing is sent.
            /// </exception>
            public async Task WriteToBlob(
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
                    throw new ArgumentOutOfRangeException(
                        nameof(serializationFormat), serializationFormat, "Unknown serialization format.");
            }
        }

        // An overwriting upload replaces all metadata, so keep the blob's existing keys (RM-12).
        private static async Task<Dictionary<string, string>> MergeFormatIntoMetadata(
            BlobClient blob,
            SerializationFormat serializationFormat,
            CancellationToken ct)
        {
            Dictionary<string, string> metadata;
            try
            {
                var properties = await blob.GetPropertiesAsync(cancellationToken: ct);
                metadata = new Dictionary<string, string>(properties.Value.Metadata);
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                metadata = new Dictionary<string, string>();
            }

            metadata[SerializationFormatKey] = serializationFormat.ToString().ToLowerInvariant();
            return metadata;
        }
    }
}
