using DreamTravel.Api;
using DreamTravel.FunctionalTests.FakeApis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SolTechnology.Core.API.Testing;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.HTTP.Testing;
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

            // 2. Start WireMock (mocks Google API) on a dynamic port — read its URL into config below.
            WireMockFixture = new WireMockFixture();
            WireMockFixture.Initialize();
            WireMockFixture.RegisterFakeApi(new GoogleFakeApi());

            var configuration = new TestConfigurationBuilder()
                .AddJsonFile("appsettings.tests.json")
                .Override("Sql:ConnectionString", SqlFixture.DatabaseConnectionString)
                .Override("HTTPClients:Google:BaseAddress", $"{WireMockFixture.Url}/google/")
                .Build();

            // Worker first — its scope factory is what the sync publisher dispatches into.
            WorkerFixture = new APIFixture<Worker.Program>(configuration);
            SyncEventPublisher.UseScopeFactory(
                () => WorkerFixture.TestServer.Services.GetRequiredService<IServiceScopeFactory>());

            // Replace the Hangfire publisher with the deterministic in-process variant.
            ApiFixture = new APIFixture<Program>(configuration, services =>
            {
                services.RemoveAll<IEventPublisher>();
                services.AddSingleton<IEventPublisher, SyncEventPublisher>();
            });
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



