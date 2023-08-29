using Xunit;

namespace DreamTravel.DatabaseDataTests.TestsConfiguration
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
