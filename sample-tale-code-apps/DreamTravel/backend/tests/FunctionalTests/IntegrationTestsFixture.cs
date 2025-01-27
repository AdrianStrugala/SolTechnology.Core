using System;
using DreamTravel.Api;
using DreamTravel.FunctionalTests.FakeApis;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SolTechnology.Core.Api.Testing;
using SolTechnology.Core.Faker;

namespace DreamTravel.FunctionalTests
{
    [SetUpFixture]
    [SetCulture("en-US")]
    public static class IntegrationTestsFixture
    {
        public static ApiFixture<Program> ApiFixture { get; set; } = null!;
        public static ApiFixture<Worker.Program> WorkerFixture { get; set; } = null!;
        public static WireMockFixture WireMockFixture { get; set; } = null!;

        [OneTimeSetUp]
        public static void SetUp()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "development");

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.integration.tests.json")
                .Build();
            
            ApiFixture = new ApiFixture<Program>(configuration);
            WorkerFixture = new ApiFixture<Worker.Program>(configuration);

            WireMockFixture = new WireMockFixture();
            WireMockFixture.Initialize();
            WireMockFixture.RegisterFakeApi(new GoogleFakeApi());
        }


        [OneTimeTearDown]
        public static void TearDown()
        {
            ApiFixture.Dispose();
            WorkerFixture.Dispose();
            WireMockFixture.Dispose();
        }
    }
}
