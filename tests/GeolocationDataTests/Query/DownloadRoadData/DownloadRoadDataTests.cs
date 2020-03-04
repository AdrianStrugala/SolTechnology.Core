using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData.Query.DownloadRoadData;
using DreamTravel.GeolocationData.Query.DownloadRoadData.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DreamTravel.GeolocationDataTests.Query.DownloadRoadData
{
    public class DownloadRoadDataTests
    {
        private readonly GeolocationData.Query.DownloadRoadData.DownloadRoadData _sut;

        public DownloadRoadDataTests()
        {
            IDownloadDurationMatrixByTollRoad downloadDurationMatrixByTollRoad = new DownloadDurationMatrixByTollRoad(NullLogger<DownloadDurationMatrixByTollRoad>.Instance);
            IDownloadDurationMatrixByFreeRoad downloadDurationMatrixByFreeRoad = new DownloadDurationMatrixByFreeRoad(NullLogger<DownloadDurationMatrixByFreeRoad>.Instance);
            IDownloadCostBetweenTwoCities downloadCostBetweenTwoCities = new DownloadCostBetweenTwoCities(NullLogger<DownloadCostBetweenTwoCities>.Instance);

            _sut = new GeolocationData.Query.DownloadRoadData.DownloadRoadData(downloadDurationMatrixByTollRoad, downloadDurationMatrixByFreeRoad, downloadCostBetweenTwoCities);
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