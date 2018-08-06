using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using Xunit;

namespace TESWebUITests.ExternalConnection
{
    public class DownloadLocationOfCityTests
    {
        private readonly DownloadLocationOfCity _sut;

        public DownloadLocationOfCityTests()
        {
            _sut = new DownloadLocationOfCity();
        }

        [Fact]
        public async Task GetCityByName_InvokeWithRealName_ReturnsCityObject()
        {
            //Arrange
            string cityName = "Wroclaw";

            //Act
            var result = await _sut.Execute(cityName);

            //Assert
            Assert.Equal("Wroclaw", result.Name);
            Assert.NotEqual(0, result.Latitude);
            Assert.NotEqual(0, result.Longitude);
        }

    }
}
