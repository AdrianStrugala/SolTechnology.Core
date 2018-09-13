using System.Collections.Generic;
using DreamTravel.BestPath.DataAccess;
using DreamTravel.BestPath.Interfaces;
using DreamTravel.BestPath.Models;
using DreamTravel.ExternalConnection;
using DreamTravel.SharedModels;
using NSubstitute;
using Xunit;

namespace DreamTravelITests.ExternalConnection
{
    public class DownloadRoadDataTests
    {
        private readonly DreamTravel.ExternalConnection.DownloadRoadData _sut;

        public DownloadRoadDataTests()
        {
            IDownloadDurationMatrixByTollRoad downloadDurationMatrixByTollRoad = Substitute.For<DownloadDurationMatrixByTollRoad>();
            IDownloadDurationMatrixByFreeRoad downloadDurationMatrixByFreeRoad = Substitute.For<DownloadDurationMatrixByFreeRoad>();
            IDownloadCostBetweenTwoCities downloadCostBetweenTwoCities = Substitute.For<DownloadCostBetweenTwoCities>();

            _sut = new DreamTravel.ExternalConnection.DownloadRoadData(downloadDurationMatrixByTollRoad, downloadDurationMatrixByFreeRoad, downloadCostBetweenTwoCities);
        }


        [Fact]
        public void DownloadExternalData_ValidConditions_MatrixIsPopulated()
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
            _sut.Execute(cities, matrix);


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
    }
}
