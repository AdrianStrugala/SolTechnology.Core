using System.IO;
using System.Threading.Tasks;
using DreamTravel.NameOfCity;
using DreamTravel.SharedModels;
using Xunit;

//Reference
//https://github.com/nchaulet/node-geocoder/blob/master/lib/geocoder/googlegeocoder.js

namespace DreamTravelITests.NameOfCity
{
    public class FindNameOfCityTests
    {
        private readonly FindNameOfCity _sut;

        public FindNameOfCityTests()
        {
            _sut = new FindNameOfCity();
        }

        [Fact]
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
            var result = await _sut.Execute(city);

            //Assert
            Assert.Equal("Kufra District, Libya", result.Name);
        }

        [Fact]
        public async Task Execute_InvokeWithValidCoordinats_ReturnsActualNameOfCity()
        {
            //Arrange
            City city = new City
            {
                Name = "",
                Latitude = 51.10788,
                Longitude = 17.0385
            };

            //Act
            var result = await _sut.Execute(city);

            //Assert
            Assert.Equal("Wrocław", result.Name);
        }

        [Fact]
        public async Task Execute_OceanUnderCoordinates_ExceptionIsThrown()
        {
            //Arrange
            City city = new City
            {
                Name = "",
                Latitude = 55,
                Longitude = 17
            };

            //Act
            // ReSharper disable once PossibleNullReferenceException
            var exception = await Record.ExceptionAsync(async () => await _sut.Execute(city));

            //Assert
            Assert.IsType<InvalidDataException>(exception);
        }

    }
}
