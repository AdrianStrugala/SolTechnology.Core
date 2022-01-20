using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.BlobStorage.Connection
{
    public class BlobConnectionFactory : IBlobConnectionFactory
    {
        private readonly string _connectionString;

        private readonly Dictionary<string, BlobContainerClient> _blobContainerCache = new Dictionary<string, BlobContainerClient>();

        public BlobConnectionFactory(IOptions<BlobStorageConfiguration> blobConfiguration)
        {
            _connectionString = blobConfiguration.Value.ConnectionString;
        }

        public BlobContainerClient CreateConnection(string containerName)
        {
            if (_blobContainerCache.ContainsKey(containerName))
            {
                return _blobContainerCache[containerName];
            }

            var blobContainerClient = new BlobContainerClient(_connectionString, containerName);
            blobContainerClient.CreateIfNotExists();
            _blobContainerCache.Add(containerName, blobContainerClient);
            return blobContainerClient;
        }
    }
}
