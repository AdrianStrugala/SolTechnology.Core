using Azure.Storage.Blobs;

namespace SolTechnology.Core.Blob.Connection
{
    /// <summary>
    /// Hands out <see cref="BlobContainerClient"/> instances for the configured storage account; registered as a
    /// singleton by <see cref="ModuleInstaller.AddSolBlob"/>.
    /// </summary>
    public interface IBlobConnectionFactory
    {
        /// <summary>
        /// Returns the cached client for <paramref name="containerName"/>. The first call for a name creates the
        /// container if it does not exist, once, even under concurrent calls; a failed creation is retried on the next call.
        /// </summary>
        /// <param name="containerName">The blob container name.</param>
        /// <returns>A client bound to the container.</returns>
        BlobContainerClient GetConnection(string containerName);
    }
}