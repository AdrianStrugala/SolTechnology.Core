using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SolTechnology.Core.BlobStorage;
using SolTechnology.Core.BlobStorage.Connection;

namespace TaleCode.IntegrationTests.Blob
{
    public class BlobFixture
    {
        public BlobConnectionFactory BlobConnectionFactory;

        public BlobFixture()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile("appsettings.development.json", true, true)
                .AddJsonFile("appsettings.tests.json", true, true)
                .Build();

            var blobConfiguration = configuration.GetRequiredSection("Configuration:BlobStorage").Get<BlobStorageConfiguration>()!;
            var options = Options.Create(blobConfiguration);

            BlobConnectionFactory = new BlobConnectionFactory(options);
        }
    }
}
