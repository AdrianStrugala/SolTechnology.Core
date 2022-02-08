using Azure.Storage.Blobs;

namespace SolTechnology.Core.BlobStorage.BlobContainerWrapper;

public interface IBlobContainerClientWrapper
{
    Task<T> ReadFromBlob<T>(
        BlobContainerClient client,
        string blobName);

    Task WriteToBlob(
        BlobContainerClient client,
        string blobName,
        object content);
}