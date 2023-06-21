using System.Text.Json;
using Microsoft.Extensions.Options;
using SolTechnology.Core.BlobStorage;
using SolTechnology.Core.BlobStorage.Connection;

namespace TaleCode.IntegrationTests.Blob
{
    public class BlobFixture
    {
        public BlobConnectionFactory BlobConnectionFactory;
        private string _connectionString;

        public BlobFixture()
        {
            var settingsFile = File.ReadAllText("appsettings.functional.tests.json");
            _connectionString = JsonDocument.Parse(settingsFile)
                .RootElement
                .GetProperty("Configuration")
                .GetProperty("BlobStorage")
                .GetProperty("ConnectionString")
                .GetString()!;

            var config = Options.Create(new BlobStorageConfiguration
            {
                ConnectionString = _connectionString
            });

            BlobConnectionFactory = new BlobConnectionFactory(config);
        }
    }
}
