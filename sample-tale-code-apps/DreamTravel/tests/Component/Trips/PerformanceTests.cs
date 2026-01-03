using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Queries.CalculateBestPath;
using FluentAssertions;
using NUnit.Framework;
// using NSubstitute;

namespace DreamTravel.FunctionalTests.Trips
{
    public class PerformanceTests
    {
        private readonly CalculateBestPathStory _sut;

        public PerformanceTests()
        {
            // IGoogleHTTPClient googleHTTPClient = new GoogleHTTPClient(Options.Create(new GoogleHTTPOptions()), Substitute.For<HttpClient>(), NullLogger<GoogleHTTPClient>.Instance);
            // IMichelinHTTPClient michelinHTTPClient = new MichelinHTTPClient(Options.Create(new MichelinHTTPOptions()), Substitute.For<HttpClient>(), NullLogger<MichelinHTTPClient>.Instance);
            //
            // DownloadRoadData downloadRoadData = new DownloadRoadData(googleApiClient, michelinApiClient);
            // IFormCalculateBestPathResult formOutputData = new FormPathsFromMatrices();
            //
            // ITSP tsp = new AntColony();
            //
            // FindProfitablePath evaluationBrain = new FindProfitablePath();
            //
            // _sut = new CalculateBestPathHandler(downloadRoadData, formOutputData, tsp, evaluationBrain);
        }

        // [Fact(Skip = "Manual test")]
        public async Task Performance_TestFor10Cities_UseWithResharperProfiling_ForFindingOptimizations()
        {
            //Arrange

            List<CalculateBestPathQuery.CityQueryModel> cities =
            [
                new() { Name = "Wroclaw", Latitude = 51, Longitude = 17, Country = "Poland" },
                new() { Name = "Warsaw", Latitude = 52, Longitude = 21, Country = "Poland" },
                new() { Name = "Gdańsk", Latitude = 54, Longitude = 18, Country = "Poland" },
                new() { Name = "Rzeszów", Latitude = 50, Longitude = 22, Country = "Poland" },
                new() { Name = "Lublin", Latitude = 51, Longitude = 22, Country = "Poland" },
                new() { Name = "Poznan", Latitude = 52, Longitude = 16, Country = "Poland" },
                new() { Name = "Kraków", Latitude = 50, Longitude = 20, Country = "Poland" },
                new() { Name = "Białystok", Latitude = 53, Longitude = 53, Country = "Poland" },
                new() { Name = "Łódź", Latitude = 51, Longitude = 19, Country = "Poland" },
                new() { Name = "Opole", Latitude = 50, Longitude = 17, Country = "Poland" }
            ];

            //Act
            var result = await _sut.Handle(new CalculateBestPathQuery { Cities = cities });


            //Assert
            result.Should().NotBeNull();
        }
    }
}
