using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SolTechnology.Core.Sql;
using SolTechnology.Core.Sql.Testing;

namespace DreamTravel.Identity.DatabaseData.IntegrationTests
{
    [SetUpFixture]
    [SetCulture("en-US")]
    public static class IntegrationTestsFixture
    {
        public static SqlFixture SqlFixture { get; set; } = null!;


        [OneTimeSetUp]
        public static async Task SetUp()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile("appsettings.development.json", true, true)
                .AddJsonFile("appsettings.tests.json", true, true)
                .Build();

            var sqlConfiguration = configuration.GetRequiredSection("Sql").Get<SqlConfiguration>()!;
            
            SqlFixture = new SqlFixture();
            await SqlFixture.Connect(sqlConfiguration);
        }


        [OneTimeTearDown]
        public static void TearDown()
        {
            SqlFixture.Dispose();
        }
    }
}
