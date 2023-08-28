using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.DreamTrips.CalculateBestPath.Executors;
using DreamTravel.GeolocationData;
using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.GeolocationData.MichelinApi;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DreamTravel.GeolocationDataTests
{
    public class DownloadRoadDataTests
    {
        private readonly DreamTrips.CalculateBestPath.Executors.DownloadRoadData _sut;

        public DownloadRoadDataTests()
        {
            IGoogleApiClient googleApiClient = new GoogleApiClient(NullLogger<GoogleApiClient>.Instance);
            IMichelinApiClient michelinApiClient = new MichelinApiClient(NullLogger<MichelinApiClient>.Instance);

            _sut = new DownloadRoadData(googleApiClient, michelinApiClient);
        }


        [Fact(Skip = "Paid test")]
        public async Task DownloadExternalData_ValidConditions_MatrixIsPopulated()
        {
            //Arrange
            City firstCity = new City
            {
                Name = "first",
                Latitude = 51,
                Longitude = 17
            };

            City secondCity = new City
            {
                Name = "second",
                Latitude = 53,
                Longitude = 19
            };

            List<City> cities = new List<City> { firstCity, secondCity };


            //Act
            EvaluationMatrix matrix = await _sut.Execute(cities);


            //Assert
            Assert.Equal(4, matrix.Costs.Length);
            Assert.Equal(4, matrix.FreeDistances.Length);
            Assert.Equal(4, matrix.TollDistances.Length);

            //valid values
            Assert.Equal(double.MaxValue, matrix.FreeDistances[0]);
            Assert.Equal(double.MaxValue, matrix.FreeDistances[3]);
            Assert.NotEqual(double.MaxValue, matrix.FreeDistances[1]);
            Assert.NotEqual(double.MaxValue, matrix.FreeDistances[2]);
        }

        //can always download data of at least 30 cities
        [Fact(Skip = "Paid test")]
        public async Task Execute_InputHas30Cities_AllTheDataIsDownloaded()
        {
            int noOfCities = 30;

            //Arrange
            City city = new City
            {
                Name = "Wroclaw",
                Latitude = 51,
                Longitude = 17
            };

            List<City> cities = new List<City>();
            for (int i = 0; i < noOfCities; i++)
            {
                cities.Add(city);
            }

            //Act
            await _sut.Execute(cities);


            //Assert

            //if test is green all the data is downloaded (no exception thrown) 
        }
    }
}