namespace SolTechnology.Core.Blob
{
    /// <summary>
    /// Settings for the Blob module, passed to <see cref="ModuleInstaller.AddSolBlob"/>.
    /// </summary>
    public class BlobConfiguration
    {
        /// <summary>
        /// The Azure Storage connection string, for example <c>UseDevelopmentStorage=true</c> for Azurite.
        /// </summary>
        public required string ConnectionString { get; set; }
    }
}
