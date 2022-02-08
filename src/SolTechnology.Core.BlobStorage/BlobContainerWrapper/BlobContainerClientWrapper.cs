using Azure.Storage.Blobs;
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

            if (_useCompression)
            {
                var serializedContent = AvroConvert.Serialize(content);
                await blob.UploadAsync(new BinaryData(serializedContent));
            }
            else
            {
                await blob.UploadAsync(BinaryData.FromObjectAsJson(content));
            }
        }
    }
}