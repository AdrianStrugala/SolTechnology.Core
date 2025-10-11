using DreamTravel.Api;
using DreamTravel.FunctionalTests.FakeApis;
using DreamTravel.Trips.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SolTechnology.Core.Api.Testing;
using SolTechnology.Core.Faker;
using SolTechnology.Core.Sql.Testing;

namespace DreamTravel.FunctionalTests
{
    [SetUpFixture]
    [SetCulture("en-US")]
    public static class ComponentTestsFixture
    {
        public static ApiFixture<Program> ApiFixture { get; set; } = null!;
        public static ApiFixture<Worker.Program> WorkerFixture { get; set; } = null!;
        public static SqlFixture SqlFixture { get; set; } = null!;
        public static WireMockFixture WireMockFixture { get; set; } = null!;

        [OneTimeSetUp]
        public static async Task SetUp()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "development");

            SqlFixture = new SqlFixture("DreamTravelDatabase")
                .WithSqlProject(Path.GetFullPath("../../../../../src/Infrastructure/DreamTravelDatabase/DreamTravelDatabase.csproj"));
            await SqlFixture.InitializeAsync();
            
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.tests.json")
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Sql:ConnectionString", SqlFixture.DatabaseConnectionString}
                })
                .Build();
            
            ApiFixture = new ApiFixture<Program>(configuration);
            WorkerFixture = new ApiFixture<Worker.Program>(configuration);
            
            WireMockFixture = new WireMockFixture();
            WireMockFixture.Initialize();
            WireMockFixture.RegisterFakeApi(new GoogleFakeApi());
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
