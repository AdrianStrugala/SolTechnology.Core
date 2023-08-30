using DreamTravel.ApiTests.TestsConfiguration;
using DreamTravel.TestFixture.Sql;
using Xunit;

namespace DreamTravel.FunctionalTests.TestsConfiguration
{
    public class ApiFixture
    {
        public HttpClientFixture InternalApiIntegrationTestsFixture { get; set; }
        public SqlFixture SqlFixture { get; set; }

        public ApiFixture()
        {
            InternalApiIntegrationTestsFixture ??= new HttpClientFixture();
            SqlFixture ??= new SqlFixture();

            SqlFixture.InitializeAsync().GetAwaiter().GetResult();
        }
    }

    [CollectionDefinition(nameof(ApiFunctionalTests))]
    public class ApiFunctionalTests : ICollectionFixture<ApiFixture>
    {
    }
}