using Xunit;

namespace TaleCode.IntegrationTests.Sql.Configuration
{
    public class TestsCollections
    {
        [CollectionDefinition(nameof(SqlTestsCollection))]
        public class SqlTestsCollection : IClassFixture<SqlFixture>
        {

        }
    }
}
