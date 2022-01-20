using Azure.Storage.Blobs;

namespace SolTechnology.Core.BlobStorage.Connection
{
    public interface IBlobConnectionFactory
    {
        BlobContainerClient CreateConnection(string containerName);
    }
}