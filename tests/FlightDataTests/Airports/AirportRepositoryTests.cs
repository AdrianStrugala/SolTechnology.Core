using System.Collections.Generic;
using System.Linq;
using DreamTravel.FlightProviderData.Airports;
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
        public void GetCityToCodesMap_FileContainingCodesIsInPlace_ListOfCodesIsReturned()
        {
            // Arrange

            // Act
            var result = _sut.GetCityToCodeMap();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            Assert.True(result.ContainsKey("Wroclaw"));
            Assert.Equal("WRO", result["Wroclaw"]);
        }

        [Fact]
        public void GetPlaceToCodesMap_FileContainingMapIsInPlace_MapOfPlacesAndCodesIsReturned()
        {
            // Arrange
            Dictionary<string, List<string>> oneOfResults = new Dictionary<string, List<string>>
            {
                {
                    "Poland", new List<string>
                    {
                        "SZY",
                        "GDN",
                        "KRK",
                        "KTW",
                        "POZ",
                        "RZE",
                        "SZZ",
                        "WAW",
                        "WRO",
                        "BZG",
                        "LCJ",
                        "WMI",
                        "LUZ"
                    }
                }
            };

            // Act
            Dictionary<string, List<string>> result = _sut.GetCountryToCodesMap();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            Assert.Equal(result[oneOfResults.Keys.First()], oneOfResults.Values.First());
        }

        [Fact]
        public void GetCodesByPlace_ValidCountry_ListOfCodesIsReturned()
        {
            // Arrange
            var expected = new List<string>
                    {
                        "SZY",
                        "GDN",
                        "KRK",
                        "KTW",
                        "POZ",
                        "RZE",
                        "SZZ",
                        "WAW",
                        "WRO",
                        "BZG",
                        "LCJ",
                        "WMI",
                        "LUZ"
                    };

            // Act
            List<string> result = _sut.GetCodesByPlace("Poland");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetCodesByPlace_ValidCity_ListOfCodesIsReturned()
        {
            // Arrange
            var expected = new List<string>
            {
                "WRO"
            };

            // Act
            List<string> result = _sut.GetCodesByPlace("Wroclaw");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            Assert.Equal(expected, result);
        }
    }
}
