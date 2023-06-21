using Azure.Storage.Blobs;

namespace SolTechnology.Core.BlobStorage.Connection
{
    public interface IBlobConnectionFactory
    {
        BlobContainerClient GetConnection(string containerName);
    }
}