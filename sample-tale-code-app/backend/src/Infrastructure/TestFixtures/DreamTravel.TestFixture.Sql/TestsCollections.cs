using Xunit;

namespace DreamTravel.TestFixture.Sql
{
    public class TestsCollections
    {
        [CollectionDefinition(nameof(SqlTestsCollection))]
        public class SqlTestsCollection : IClassFixture<SqlFixture>
        {

        }
    }
}
