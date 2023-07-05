using System;
using TaleCode.IntegrationTests.Api;
using TaleCode.IntegrationTests.Blob;
using TaleCode.IntegrationTests.Sql.Configuration;
using Xunit;

namespace TaleCode.FunctionalTests.TestsConfiguration
{
    public class FunctionalTestsFixture : IDisposable
    {
        public BackgroundWorkerFixture BackgroundWorkerFixture { get; set; }
        public ApiFixture ApiFixture { get; set; }
        public SqlFixture SqlFixture { get; set; }
        public BlobFixture BlobFixture { get; set; }
        public WireMockFixture WireMockFixture { get; set; }

        public FunctionalTestsFixture()
        {
            BackgroundWorkerFixture ??= new BackgroundWorkerFixture();
            ApiFixture ??= new ApiFixture();
            SqlFixture ??= new SqlFixture();
            WireMockFixture ??= new WireMockFixture();
            BlobFixture ??= new BlobFixture();

            SqlFixture.InitializeAsync().GetAwaiter().GetResult();
            WireMockFixture.Initialize();
        }

        public void Dispose()
        {
            BackgroundWorkerFixture.Dispose();
            ApiFixture.Dispose();
            SqlFixture.DisposeAsync().GetAwaiter().GetResult();
            WireMockFixture.Dispose();
        }
    }

    [CollectionDefinition(nameof(TaleCodeFunctionalTestsCollection), DisableParallelization = true)]
    public class TaleCodeFunctionalTestsCollection : ICollectionFixture<FunctionalTestsFixture>
    {
    }

}
