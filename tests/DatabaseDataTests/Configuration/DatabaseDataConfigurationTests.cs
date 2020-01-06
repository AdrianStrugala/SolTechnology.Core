using DreamTravel.DatabaseData.Configuration;
using Xunit;

namespace DreamTravel.DatabaseDataTests.Configuration
{
    public class DatabaseDataConfigurationTests
    {
        private readonly DatabaseDataConfiguration _sut;

        public DatabaseDataConfigurationTests()
        {
            _sut = new DatabaseDataConfiguration();
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
