using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.GeolocationData.MichelinApi;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.CalculateBestPath;
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Xunit;

namespace DreamTravel.FunctionalTests.Trips
{
    public class DownloadRoadDataTests
    {
        private readonly DownloadRoadData _sut;

        public DownloadRoadDataTests()
        {
            IGoogleApiClient googleApiClient = new GoogleApiClient(Options.Create(new GoogleApiOptions()), Substitute.For<HttpClient>(), NullLogger<GoogleApiClient>.Instance);
            IMichelinApiClient michelinApiClient = new MichelinApiClient(Options.Create(new MichelinApiOptions()), Substitute.For<HttpClient>(), NullLogger<MichelinApiClient>.Instance);

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

            var context = new CalculateBestPathContext(cities);

            //Act
            await _sut.Execute(context);


            //Assert
            Assert.Equals(4, context.Costs.Length);
            Assert.Equals(4, context.FreeDistances.Length);
            Assert.Equals(4, context.TollDistances.Length);

            //valid values
            Assert.Equals(double.MaxValue, context.FreeDistances[0]);
            Assert.Equals(double.MaxValue, context.FreeDistances[3]);
            Assert.Equals(double.MaxValue, context.FreeDistances[1]);
            Assert.Equals(double.MaxValue, context.FreeDistances[2]);
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

            var context = new CalculateBestPathContext(cities);


            //Act
            await _sut.Execute(context);


            //Assert

            //if test is green all the data is downloaded (no exception thrown) 
        }
    }
}