using DreamTravel.DatabaseData.Configuration;
using DreamTravel.Infrastructure.Database;
using Xunit;

namespace DreamTravel.DatabaseDataTests.Configuration
{
    public class DatabaseDataConfigurationTests
    {
        private readonly SqlDatabaseConfiguration _sut;

        public DatabaseDataConfigurationTests()
        {
            _sut = new SqlDatabaseConfiguration();
        }

        [Fact]
        public void ConnectionString_ContainsReferenceToDemoDatabase()
        {
            //Arrange


            //Act


            //Assert
            Assert.Contains("dreamtravel-demo", _sut.ConnectionString);
        }
    }
}
