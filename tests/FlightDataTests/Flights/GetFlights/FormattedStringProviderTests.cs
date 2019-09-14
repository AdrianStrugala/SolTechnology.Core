using System.Collections.Generic;
using System.Linq;
using DreamTravel.FlightProviderData.Flights.GetFlights;
using Xunit;

namespace DreamTravel.FlightDataTests.Flights.GetFlights
{
    public class FormattedStringProviderTests
    {

        [Fact]
        public void Airports_SingleAirport_ResultIsAsExpected()
        {
            // Arrange
            string expected = "%5BWRO%5D";

            List<string> airports = new List<string> { "WRO" };

            // Act
            string result = FormattedStringProvider.Airports(airports);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetPlaceToCodesMap_FileContainingMapIsInPlace_MapOfPlacesAndCodesIsReturned()
        {
            // Arrange
            string expected = "%5BSZY%5D+%28%2BWMI%2CWAW%2CGDN%2CBZG%2CLCJ%2CLUZ%2CPOZ%2CKRK%2CKTW%2CRZE%2CSZZ%2CWRO%29";

            List<string> airports = new List<string> {
                "SZY",
                "WMI",
                "WAW",
                "GDN",
                "BZG",
                "LCJ",
                "LUZ",
                "POZ",
                "KRK",
                "KTW",
                "RZE",
                "SZZ",
                "WRO"
           };

            // Act
            string result = FormattedStringProvider.Airports(airports);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
