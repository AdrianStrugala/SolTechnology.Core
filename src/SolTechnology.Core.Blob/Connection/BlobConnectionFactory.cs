using System.Collections.Concurrent;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Blob.Connection
{
    internal sealed class BlobConnectionFactory(IOptions<BlobConfiguration> blobConfiguration) : IBlobConnectionFactory
    {
        private readonly string _connectionString = blobConfiguration.Value.ConnectionString;

        private readonly ConcurrentDictionary<string, Lazy<Task<BlobContainerClient>>> _blobContainerCache = new();

        public BlobContainerClient GetConnection(string containerName)
        {
            var connection = _blobContainerCache.GetOrAdd(
                containerName,
                name => new Lazy<Task<BlobContainerClient>>(() => CreateContainer(name)));

            try
            {
                return connection.Value.GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // Evict only this failed entry, so the next call retries the creation.
                _blobContainerCache.TryRemove(new KeyValuePair<string, Lazy<Task<BlobContainerClient>>>(containerName, connection));
                throw;
            }
        }

        private async Task<BlobContainerClient> CreateContainer(string containerName)
        {
            var blobContainerClient = new BlobContainerClient(_connectionString, containerName);
            await blobContainerClient.CreateIfNotExistsAsync().ConfigureAwait(false);
            return blobContainerClient;
        }
    }
}
