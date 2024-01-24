using SolTechnology.Core.Api.Testing;
using System;
using SolTechnology.Core.Faker;
using SolTechnology.Core.Sql.Testing;
using TaleCode.IntegrationTests.Blob;
using Xunit;
using TaleCode.FunctionalTests.FakeApis;

namespace TaleCode.FunctionalTests.TestsConfiguration
{
    public class FunctionalTestsFixture : IDisposable
    {
        public ApiFixture<SolTechnology.TaleCode.BackgroundWorker.Program> BackgroundWorkerFixture { get; set; }
        public ApiFixture<Program> ApiFixture { get; set; }
        public SqlFixture SqlFixture { get; set; }
        public BlobFixture BlobFixture { get; set; }
        public WireMockFixture WireMockFixture { get; set; }

        public FunctionalTestsFixture()
        {
            BackgroundWorkerFixture ??= new ApiFixture<SolTechnology.TaleCode.BackgroundWorker.Program>();
            ApiFixture ??= new ApiFixture<Program>();
            SqlFixture ??= new SqlFixture();
            WireMockFixture ??= new WireMockFixture();
            BlobFixture ??= new BlobFixture();

            SqlFixture.InitializeAsync().GetAwaiter().GetResult();
            WireMockFixture.Initialize();
            WireMockFixture.RegisterFakeApi(new ApiFootballFakeApi());
            WireMockFixture.RegisterFakeApi(new FootballDataFakeApi());
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
