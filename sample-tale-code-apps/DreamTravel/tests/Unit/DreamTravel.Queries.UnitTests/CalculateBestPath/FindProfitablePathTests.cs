using DreamTravel.Domain.Cities;
using DreamTravel.Queries.CalculateBestPath;
using DreamTravel.Queries.CalculateBestPath.Chapters;
using FluentAssertions;
using SolTechnology.Core.CQRS;
namespace DreamTravel.Queries.UnitTests.CalculateBestPath
{
    [TestFixture]
    public class FindProfitablePathTests
    {
        private readonly FindProfitablePath _sut = new();

        [Test]
        public async Task EvaluateCost_ValidData_IntelligentResults()
        {
            //Arrange
            var cities = new List<City>
            {
                new City { Name = "CityA" },
                new City { Name = "CityB" },
                new City { Name = "CityC" }
            };

            int expectedLength = cities.Count * cities.Count; // 3 x 3 = 9
            var matrix = new CalculateBestPathContext
            {
                Cities = cities,
                NoOfCities = cities.Count,
                VinietaCosts = new double[expectedLength],
                OptimalDistances = new double[expectedLength],
                OptimalCosts = new double[expectedLength],
                Goals = new double[expectedLength],
                Costs =
                [
                    Double.MaxValue, 10, 19,
                    10, Double.MaxValue, 30,
                    19, 30,  Double.MaxValue
                ],
                FreeDistances =
                [
                    Double.MaxValue, 100, 200,
                    100, Double.MaxValue, 300,
                    200, 300,  Double.MaxValue
                ],
                TollDistances =
                [
                    Double.MaxValue, 90, 20,
                    90, Double.MaxValue, 350,
                    20, 350,  Double.MaxValue
                ]
            };


            //Act
            await _sut.Read(matrix);


            //Assert
            //1) takes profitable road
            matrix.OptimalDistances[2].Should().Be(20);
            matrix.OptimalCosts[2].Should().Be(19);
            matrix.OptimalDistances[6].Should().Be(20);
            matrix.OptimalCosts[6].Should().Be(19);

            //2) Rejects non-profitable road
            matrix.OptimalDistances[1].Should().Be(100);
            matrix.OptimalCosts[1].Should().Be(0);
            matrix.OptimalDistances[3].Should().Be(100);
            matrix.OptimalCosts[3].Should().Be(0);

            //3) Rejects toll road longer than free
            matrix.OptimalDistances[5].Should().Be(300);
            matrix.OptimalCosts[5].Should().Be(0);
            matrix.OptimalDistances[7].Should().Be(300);
            matrix.OptimalCosts[7].Should().Be(0);
        }
    }
}
