using System;
using DreamTravel.Api;
using DreamTravel.FunctionalTests.FakeApis;
using NUnit.Framework;
using SolTechnology.Core.Api.Testing;
using SolTechnology.Core.Faker;

namespace DreamTravel.FunctionalTests
{
    [SetUpFixture]
    [SetCulture("en-US")]
    public static class IntegrationTestsFixture
    {
        public static ApiFixture<Program> ApiFixture { get; set; }
        public static WireMockFixture WireMockFixture { get; set; }

        [OneTimeSetUp]
        public static void SetUp()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "development");

            ApiFixture = new ApiFixture<Program>();
            WireMockFixture = new WireMockFixture();

            WireMockFixture.Initialize();
            WireMockFixture.RegisterFakeApi(new GoogleFakeApi());
        }

        [OneTimeTearDown]
        public static void TearDown()
        {
            ApiFixture.Dispose();
            WireMockFixture.Dispose();
        }
    }
}
