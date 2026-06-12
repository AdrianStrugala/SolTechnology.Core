//Reference
//https://github.com/nchaulet/node-geocoder/blob/master/lib/geocoder/googlegeocoder.js


using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationDataClients.GoogleApi;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace DreamTravel.GeolocationDataClients.IntegrationTests.GoogleApi
{
    [TestFixture]
    public class CitiesTests
    {
        private GoogleHTTPClient _sut = null!;

        [SetUp]
        public void Setup()
        {
            _sut = new GoogleHTTPClient(Options.Create(new GoogleHTTPOptions()), Substitute.For<HttpClient>(), NullLogger<GoogleHTTPClient>.Instance);
        }

        [Test, Ignore("Paid test")]
        public async Task Execute_WildernessUnderCoordinates_ReturnsFormattedAddress()
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
            result.Name.Should().Be("Kufra District, Libya");
        }

        [Test, Ignore("Paid test")]
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
            result.Name.Should().Be("Wrocław");
        }

        [Test, Ignore("Paid test")]
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
            result.Name.Should().Be("Baltic Sea");
        }

        [Test, Ignore("Paid test")]
        public async Task GetCityByName_InvokeWithRealName_ReturnsCityObject()
        {
            //Arrange
            string cityName = "Wroclaw";

            //Act
            var result = await _sut.GetLocationOfCity(cityName);

            //Assert
            result.Name.Should().Be("Wroclaw");
            result.Latitude.Should().NotBe(0);
            result.Longitude.Should().NotBe(0);
        }

        [Test, Ignore("Paid test")]
        public async Task GetCityByName_NonExistingCity_ExceptionIsThrown()
        {
            //Arrange
            string cityName = "DUPA";

            //Act
            var act = async () => await _sut.GetLocationOfCity(cityName);

            //Assert
            await act.Should().ThrowAsync<InvalidDataException>();
        }

    }
}
