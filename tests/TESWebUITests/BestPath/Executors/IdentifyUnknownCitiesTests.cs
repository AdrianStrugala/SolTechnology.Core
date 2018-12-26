namespace DreamTravelITests.BestPath.Executors
{
    using AutoFixture;
    using DreamTravel.BestPath.Executors;
    using DreamTravel.SharedModels;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class IdentifyUnknownCitiesTests : IClassFixture<City>
    {
        private readonly IdentifyUnknownCities _sut;
        private readonly Fixture _fixture;

        public IdentifyUnknownCitiesTests()
        {
            _sut = new IdentifyUnknownCities();
            _fixture = new Fixture();
        }

        [Fact]
        public void Execute_NoKnownCities_ReturnsAllNewCities()
        {
            //Arrange
            List<City> newCities = _fixture.CreateMany<City>().ToList();
            List<City> knownCities = new List<City>();


            //Act
            var result = _sut.Execute(newCities, knownCities);


            //Assert
            Assert.Equal(newCities.Count, result.Count);
        }

        [Fact]
        public void Execute_SomeCitiesAreKnown_ReturnsCitiesOtherThanKnown()
        {
            //Arrange
            List<City> newCities = _fixture.CreateMany<City>(5).ToList();

            List<City> knownCities = new List<City>();
            knownCities.Add(newCities[0]);
            knownCities.Add(newCities[1]);
            knownCities.Add(newCities[3]);


            //Act
            var result = _sut.Execute(newCities, knownCities);


            //Assert
            Assert.Equal(newCities.Count - knownCities.Count, result.Count);

            Assert.Equal(1, result.Count(c => c.Name == newCities[2].Name));
            Assert.Equal(1, result.Count(c => c.Name == newCities[4].Name));

            Assert.Equal(0, result.Count(c => c.Name == newCities[0].Name));
            Assert.Equal(0, result.Count(c => c.Name == newCities[1].Name));
            Assert.Equal(0, result.Count(c => c.Name == newCities[3].Name));
        }

        [Fact]
        public void Execute_KnownCitiesContainsMoreCitiesThanNew_ReturnsProperNewCities()
        {
            //Arrange
            List<City> newCities = _fixture.CreateMany<City>(3).ToList();

            List<City> knownCities = _fixture.CreateMany<City>(5).ToList();
            knownCities.Add(newCities[0]);


            //Act
            var result = _sut.Execute(newCities, knownCities);


            //Assert
            Assert.Equal(newCities.Count - 1, result.Count);
        }
    }
}
