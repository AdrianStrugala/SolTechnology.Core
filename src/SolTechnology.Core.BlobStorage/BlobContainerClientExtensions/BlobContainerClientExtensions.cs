using Azure.Storage.Blobs;
using SolTechnology.Avro;

namespace SolTechnology.Core.BlobStorage.BlobContainerClientExtensions
{
    public static class BlobContainerClientExtensions
    {

        public static async Task<T> ReadFromBlob<T>(
            this BlobContainerClient client,
            string blobName)
        {
            var blob = client.GetBlobClient(blobName);
            var content = await blob.DownloadContentAsync();

            return AvroConvert.Deserialize<T>(content.Value.Content.ToArray());

        }

        public static async Task WriteToBlob(
            this BlobContainerClient client,
            string blobName,
            object content)
        {
            var blob = client.GetBlobClient(blobName);
            var serializedContent = AvroConvert.Serialize(content, CodecType.Brotli);
            
            await blob.UploadAsync(new MemoryStream(serializedContent), true, CancellationToken.None);
        }
    }
}