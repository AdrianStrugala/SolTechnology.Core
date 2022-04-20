using Xunit;

namespace TaleCode.IntegrationTests.SqlData
{
    public class TestsCollections
    {
        [CollectionDefinition(nameof(SqlTestsCollection))]
        public class SqlTestsCollection : IClassFixture<SqlFixture>
        {

        }
    }
}
