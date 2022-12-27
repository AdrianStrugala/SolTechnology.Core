using SolTechnology.Avro;
using SolTechnology.Core.BlobStorage;

// ReSharper disable once CheckNamespace
namespace Azure.Storage.Blobs
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

            Dictionary<string, string> metadata;
            if (await blob.ExistsAsync())
            {
                var properties = await blob.GetPropertiesAsync();
                metadata = new Dictionary<string, string>(properties.Value.Metadata);
            }
            else
            {
                metadata = new Dictionary<string, string>();
            }
            metadata[SerializationFormatKey] = $"{serializationFormat.ToString().ToLowerInvariant()}";

            await blob.UploadAsync(new MemoryStream(serializedContent), true, CancellationToken.None);
            await blob.SetMetadataAsync(metadata);
        }
    }
}