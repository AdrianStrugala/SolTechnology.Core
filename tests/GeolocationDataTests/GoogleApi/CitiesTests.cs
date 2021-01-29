//Reference
//https://github.com/nchaulet/node-geocoder/blob/master/lib/geocoder/googlegeocoder.js


using System.IO;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData.GoogleApi;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DreamTravel.GeolocationDataTests.GoogleApi
{
    public class CitiesTests
    {
        private readonly GoogleApiClient _sut;

        public CitiesTests()
        {
            _sut = new GoogleApiClient(NullLogger<GoogleApiClient>.Instance);
        }

        [Fact (Skip = "Paid test")]
        public async Task Execute_WildernessUnderCoordinats_ReturnsFormattedAddress()
        {
            //Arrange
            City city = new City
            {
                Name = "",
                Latitude = 24,
                Longitude = 20
            };

            //Act
            var result = await _sut.GetNameOfCity(city);

            //Assert
            Assert.Equal("Kufra District, Libya", result.Name);
        }

        [Fact(Skip = "Paid test")]
        public async Task Execute_InvokeWithValidCoordinates_ReturnsActualNameOfCity()
        {
            //Arrange
            City city = new City
            {
                Name = "",
                Latitude = 51.10788,
                Longitude = 17.0385
            };

            //Act
            var result = await _sut.GetNameOfCity(city);

            //Assert
            Assert.Equal("Wrocław", result.Name);
        }

        [Fact(Skip = "Paid test")]
        public async Task Execute_SeeUnderCoordinates_NameOfTheSeeIsReturned()
        {
            //Arrange
            City city = new City
            {
                Name = "",
                Latitude = 55,
                Longitude = 17
            };

            //Act
            var result = await _sut.GetNameOfCity(city);

            //Assert
            Assert.Equal("Baltic Sea", result.Name);
        }

        [Fact(Skip = "Paid test")]
        public async Task GetCityByName_InvokeWithRealName_ReturnsCityObject()
        {
            //Arrange
            string cityName = "Wroclaw";

            //Act
            var result = await _sut.GetLocationOfCity(cityName);

            //Assert
            Assert.Equal("Wroclaw", result.Name);
            Assert.NotEqual(0, result.Latitude);
            Assert.NotEqual(0, result.Longitude);
        }

        [Fact(Skip = "Paid test")]
        public async Task GetCityByName_NonExistingCity_ExceptionIsThrown()
        {
            //Arrange
            string cityName = "DUPA";

            //Act
            // ReSharper disable once PossibleNullReferenceException
            var exception = await Record.ExceptionAsync(async () => await _sut.GetLocationOfCity(cityName));

            //Assert
            Assert.IsType<InvalidDataException>(exception);
        }

    }
}
