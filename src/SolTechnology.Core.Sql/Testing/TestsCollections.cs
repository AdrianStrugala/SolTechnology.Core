using Xunit;

namespace SolTechnology.Core.Sql.Testing
{
    public class TestsCollections
    {
        [CollectionDefinition(nameof(SqlTestsCollection))]
        public class SqlTestsCollection : IClassFixture<SqlFixture>
        {

        }
    }
}
