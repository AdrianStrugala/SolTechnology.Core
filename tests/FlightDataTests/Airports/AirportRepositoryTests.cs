using System.Collections.Generic;
using DreamTravel.FlightData.Airports;
using Xunit;

namespace DreamTravel.FlightDataTests.Airports
{
    public class AirportRepositoryTests
    {
        private readonly AirportRepository _sut;

        public AirportRepositoryTests()
        {
            _sut = new AirportRepository();
        }

        [Fact]
        public void GetCodes_FileContainingCodesIsInPlace_ListOfCodesIsReturned()
        {
            // Arrange

            // Act
            List<string> result = _sut.GetCodes();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            Assert.Contains("WRO", result);
        }
    }
}
