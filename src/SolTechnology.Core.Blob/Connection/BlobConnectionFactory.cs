using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Blob.Connection
{
    public class BlobConnectionFactory : IBlobConnectionFactory
    {
        private readonly string _connectionString;

        private readonly Dictionary<string, BlobContainerClient> _blobContainerCache = new();

        public BlobConnectionFactory(IOptions<BlobConfiguration> blobConfiguration)
        {
            _connectionString = blobConfiguration.Value.ConnectionString;
        }

        public BlobContainerClient GetConnection(string containerName)
        {
            if (_blobContainerCache.TryGetValue(containerName, out var connection))
            {
                return connection;
            }

            var blobContainerClient = new BlobContainerClient(_connectionString, containerName);
            blobContainerClient.CreateIfNotExists();
            _blobContainerCache.Add(containerName, blobContainerClient);
            return blobContainerClient;
        }
    }
}
