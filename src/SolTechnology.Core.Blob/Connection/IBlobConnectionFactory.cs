using Azure.Storage.Blobs;

namespace SolTechnology.Core.Blob.Connection
{
    public interface IBlobConnectionFactory
    {
        BlobContainerClient GetConnection(string containerName);
    }
}