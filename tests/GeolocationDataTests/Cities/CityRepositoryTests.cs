//Reference
//https://github.com/nchaulet/node-geocoder/blob/master/lib/geocoder/googlegeocoder.js


using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData.Cities;
using Xunit;

namespace DreamTravel.GeolocationDataTests.Cities
{
    public class CityRepositoryTests
    {
        private readonly CityRepository _sut;

        public CityRepositoryTests()
        {
            _sut = new CityRepository();
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
            var result = await _sut.GetName(city);

            //Assert
            Assert.Equal("Kufra District, Libya", result.Name);
        }

        [Fact]
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
            var result = await _sut.GetName(city);

            //Assert
            Assert.Equal("Wrocław", result.Name);
        }

        [Fact]
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
            var result = await _sut.GetName(city);

            //Assert
            Assert.Equal("Baltic Sea", result.Name);
        }

    }
}
