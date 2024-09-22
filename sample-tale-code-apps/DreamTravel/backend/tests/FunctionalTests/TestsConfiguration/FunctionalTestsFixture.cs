using System;
using DreamTravel.Api;
using DreamTravel.FunctionalTests.FakeApis;
using SolTechnology.Core.Api.Testing;
using SolTechnology.Core.Faker;
using Xunit;

namespace DreamTravel.FunctionalTests.TestsConfiguration
{
    public class FunctionalTestsFixture : IDisposable
    {
        public ApiFixture<Program> ApiFixture { get; set; }
        public WireMockFixture WireMockFixture { get; set; }

        public FunctionalTestsFixture()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "development");

            ApiFixture = new ApiFixture<DreamTravel.Api.Program>();
            WireMockFixture = new WireMockFixture();

            WireMockFixture.Initialize();
            WireMockFixture.RegisterFakeApi(new GoogleFakeApi());
        }

        public void Dispose()
        {
            ApiFixture.Dispose();
            WireMockFixture.Dispose();
        }
    }

    [CollectionDefinition(nameof(DreamTravelFunctionalTestsCollection), DisableParallelization = true)]
    public class DreamTravelFunctionalTestsCollection : ICollectionFixture<FunctionalTestsFixture>
    {
    }

}
