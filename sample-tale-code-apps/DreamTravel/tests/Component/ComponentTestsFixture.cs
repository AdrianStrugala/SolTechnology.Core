using DreamTravel.Api;
using DreamTravel.FunctionalTests.FakeApis;
using DreamTravel.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SolTechnology.Core.API.Testing;
using SolTechnology.Core.Faker;
using SolTechnology.Core.SQL.Testing;

namespace DreamTravel.FunctionalTests
{
    [SetUpFixture]
    [SetCulture("en-US")]
    public static class ComponentTestsFixture
    {
        // In-memory fixtures (for API integration tests)
        public static APIFixture<Program> ApiFixture { get; set; } = null!;
        public static APIFixture<Worker.Program> WorkerFixture { get; set; } = null!;

        // Shared fixtures
        public static SQLFixture SqlFixture { get; set; } = null!;
        public static WireMockFixture WireMockFixture { get; set; } = null!;

        [OneTimeSetUp]
        public static async Task SetUp()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "development");

            // 1. Start SQL Server (Docker container)
            SqlFixture = new SQLFixture("DreamTravelDatabase")
                .WithSQLProject(Path.GetFullPath("../../../../../src/Infrastructure/DreamTravelDatabase/DreamTravelDatabase.csproj"));
            await SqlFixture.InitializeAsync();

            // 2. Start WireMock (mocks Google API) on port 2137
            WireMockFixture = new WireMockFixture();
            WireMockFixture.Initialize();
            WireMockFixture.RegisterFakeApi(new GoogleFakeApi());

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.tests.json")
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Sql:ConnectionString", SqlFixture.DatabaseConnectionString}
                })
                .Build();

            // 3. Start in-memory API fixtures (for API integration tests)
            ApiFixture = new APIFixture<Program>(configuration);
            WorkerFixture = new APIFixture<Worker.Program>(configuration);
        }


        [OneTimeTearDown]
        public static async Task TearDown()
        {
            await SqlFixture.DisposeAsync();
            ApiFixture.Dispose();
            WorkerFixture.Dispose();
            WireMockFixture.Dispose();
        }
    }
}
