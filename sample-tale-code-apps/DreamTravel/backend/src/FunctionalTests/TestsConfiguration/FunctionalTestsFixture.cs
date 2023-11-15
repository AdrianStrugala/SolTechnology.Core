using System;
using DreamTravel.Api;
using DreamTravel.TestFixture.Sql;
using SolTechnology.Core.Api.Testing;
using Xunit;

namespace DreamTravel.FunctionalTests.TestsConfiguration
{
    public class FunctionalTestsFixture : IDisposable
    {
        // public BackgroundWorkerFixture BackgroundWorkerFixture { get; set; }
        public ApiFixture<Program> ApiFixture { get; set; }
        // public SqlFixture SqlFixture { get; set; }
        // public BlobFixture BlobFixture { get; set; }
        // public WireMockFixture WireMockFixture { get; set; }

        public FunctionalTestsFixture()
        {
            // BackgroundWorkerFixture ??= new BackgroundWorkerFixture();
            ApiFixture = new ApiFixture<Program>();
            // SqlFixture = new SqlFixture();
            // WireMockFixture ??= new WireMockFixture();
            // BlobFixture ??= new BlobFixture();

            // SqlFixture.InitializeAsync().GetAwaiter().GetResult();
            // WireMockFixture.Initialize();
        }

        public void Dispose()
        {
            // BackgroundWorkerFixture.Dispose();
            ApiFixture.Dispose();
            // SqlFixture.DisposeAsync().GetAwaiter().GetResult();
            // WireMockFixture.Dispose();
        }
    }

    [CollectionDefinition(nameof(DreamTravelFunctionalTestsCollection), DisableParallelization = true)]
    public class DreamTravelFunctionalTestsCollection : ICollectionFixture<FunctionalTestsFixture>
    {
    }

}
