using Azure.Storage.Blobs;
using SolTechnology.Avro;

namespace SolTechnology.Core.BlobStorage.BlobContainerClientExtensions
{
    public static class BlobContainerClientExtensions
    {
        private const string SerializationFormatKey = "SerializationFormat";

        public static async Task<T> ReadFromBlob<T>(
            this BlobContainerClient client,
            string blobName)
        {
            var blob = client.GetBlobClient(blobName);
            var content = await blob.DownloadContentAsync();

            var properties = await blob.GetPropertiesAsync();
            var metadata = properties.Value.Metadata ?? new Dictionary<string, string>();
            metadata.TryGetValue(SerializationFormatKey, out var contentType);

            switch (contentType)
            {
                case "avro":
                    return AvroConvert.Deserialize<T>(content.Value.Content.ToArray());

                case "json":
                default:
                    return content.Value.Content.ToObjectFromJson<T>();
            }
        }

        public static async Task WriteToBlob(
            this BlobContainerClient client,
            string blobName,
            object content)
        {
            await WriteToBlob(client, blobName, content, SerializationFormat.Json);
        }

        public static async Task WriteToBlob(
            this BlobContainerClient client,
            string blobName,
            object content,
            SerializationFormat serializationFormat)
        {
            var blob = client.GetBlobClient(blobName);

            byte[] serializedContent = { };
            switch (serializationFormat)
            {
                case SerializationFormat.Json:
                    serializedContent = BinaryData.FromObjectAsJson(content).ToArray();
                    break;

                case SerializationFormat.Avro:
                    serializedContent = AvroConvert.Serialize(content, CodecType.Brotli);
                    break;
            }

            var properties = await blob.GetPropertiesAsync();

            var metadata = properties.Value.Metadata ?? new Dictionary<string, string>();
            metadata[SerializationFormatKey] = $"{serializationFormat.ToString().ToLowerInvariant()}";

            await blob.UploadAsync(new MemoryStream(serializedContent), true, CancellationToken.None);
            await blob.SetMetadataAsync(metadata);
        }
    }
}