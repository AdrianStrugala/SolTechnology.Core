using Xunit;

namespace DreamTravel.Identity.DatabaseData.IntegrationTests.TestsConfiguration
{
    public class TestsCollections
    {
        [CollectionDefinition(nameof(SqlTestsCollection))]
        public class SqlTestsCollection :
            IClassFixture<SqlFixture>
        {

        }
    }
}
