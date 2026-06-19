using DreamTravel.Domain.Cities;
using DreamTravel.Queries.CalculateBestPath;
using DreamTravel.Queries.CalculateBestPath.Chapters;
using FluentAssertions;
using SolTechnology.Core;

namespace DreamTravel.Queries.UnitTests.CalculateBestPath
{
    [TestFixture]
    public class FormCalculateBestPathResultTests
    {
        private readonly FormCalculateBestPathResult _sut = new FormCalculateBestPathResult();

        [Test]
        public async Task GetDurationBetweenTwoCitiesByTollRoad_InvokeWithValidCities_ReturnsSomeDuration()
        {
            //Arrange
            int noOfCities = 3;

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

            City thirdCity = new City
            {
                Name = "third",
                Latitude = 55,
                Longitude = 21
            };
            List<City> listOfCities = new List<City> { firstCity, secondCity, thirdCity };

            List<int> orderOfCities = new List<int>(noOfCities) { 0, 2, 1 };

            var calculateBestPathContext = new CalculateBestPathContext
            {
                Cities = listOfCities,
                Costs = new double[noOfCities*noOfCities],
                Goals = new double[noOfCities*noOfCities],
                VinietaCosts = new double[noOfCities*noOfCities],
                OptimalCosts = new double[noOfCities*noOfCities],
                OptimalDistances = new double[noOfCities*noOfCities],
                FreeDistances = new double[noOfCities*noOfCities],
                TollDistances = new double[noOfCities*noOfCities],
                OrderOfCities = orderOfCities
            };

            //Act
            Result result = await _sut.Read(calculateBestPathContext);

            //Assert
            result.IsSuccess.Should().BeTrue();
            var bestPaths = calculateBestPathContext.Output.BestPaths;
            bestPaths.Should().HaveCount(noOfCities - 1);
            bestPaths[0].StartingCity.Name.Should().Be("first");
            bestPaths[1].StartingCity.Name.Should().Be("third");
            bestPaths[1].EndingCity.Name.Should().Be("second");
        }
    }
}
