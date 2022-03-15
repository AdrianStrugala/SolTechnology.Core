using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using SolTechnology.Avro;

namespace SolTechnology.Core.BlobStorage.BlobContainerWrapper
{
    public class BlobContainerClientWrapper : IBlobContainerClientWrapper
    {
        private readonly bool _useCompression;

        public BlobContainerClientWrapper(IOptions<BlobStorageConfiguration> blobConfiguration)
        {
            _useCompression = blobConfiguration.Value.UseCompression;
        }

        public async Task<T> ReadFromBlob<T>(
            BlobContainerClient client,
            string blobName)
        {
            var blob = client.GetBlobClient(blobName);
            var content = await blob.DownloadContentAsync();

            if (_useCompression)
            {
                return AvroConvert.Deserialize<T>(content.Value.Content.ToArray());
            }
            else
            {
                return content.Value.Content.ToObjectFromJson<T>();
            }
        }

        public async Task WriteToBlob(
            BlobContainerClient client,
            string blobName,
            object content)
        {
            var blob = client.GetBlobClient(blobName);

            byte[] serializedContent;

            if (_useCompression)
            {
                serializedContent = AvroConvert.Serialize(content);

            }
            else
            {
                serializedContent = BinaryData.FromObjectAsJson(content).ToArray();
            }

            await blob.UploadAsync(new MemoryStream(serializedContent), true, CancellationToken.None);
        }
    }
}