namespace DreamTravelITests.BestPath.Executors
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DreamTravel.BestPath.DataAccess;
    using DreamTravel.BestPath.Executors;
    using DreamTravel.BestPath.Interfaces;
    using DreamTravel.BestPath.Models;
    using DreamTravel.SharedModels;
    using NSubstitute;
    using Xunit;

    public class DownloadRoadDataTests
    {
        private readonly DownloadRoadData _sut;

        public DownloadRoadDataTests()
        {
            IDownloadDurationMatrixByTollRoad downloadDurationMatrixByTollRoad = Substitute.For<DownloadDurationMatrixByTollRoad>();
            IDownloadDurationMatrixByFreeRoad downloadDurationMatrixByFreeRoad = Substitute.For<DownloadDurationMatrixByFreeRoad>();
            IDownloadCostBetweenTwoCities downloadCostBetweenTwoCities = Substitute.For<DownloadCostBetweenTwoCities>();

            _sut = new DownloadRoadData(downloadDurationMatrixByTollRoad, downloadDurationMatrixByFreeRoad, downloadCostBetweenTwoCities);
        }


        [Fact]
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

            EvaluationMatrix matrix = new EvaluationMatrix(2);


            //Act
            matrix = await _sut.Execute(cities, matrix);


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
        [Fact]
        public async Task Execute_InputHas30Cities_AllTheDataIsDownloaded()
        {
            int noOfCities = 30;

            //Arrange
            City city = new City
            {
                Name = "name",
                Latitude = 51,
                Longitude = 17
            };

            List<City> cities = new List<City>();
            for (int i = 0; i < noOfCities; i++)
            {
                cities.Add(city);
            }

            EvaluationMatrix matrix = new EvaluationMatrix(noOfCities);


            //Act
            await _sut.Execute(cities, matrix);


            //Assert

            //if test is green all the data is downloaded (no exception thrown) 
        }
    }
}
