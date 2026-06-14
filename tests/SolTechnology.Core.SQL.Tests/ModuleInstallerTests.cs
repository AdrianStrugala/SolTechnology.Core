using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.SQL.Connections;
using NUnit.Framework;

namespace SolTechnology.Core.SQL.Tests
{
    [TestFixture]
    public class ModuleInstallerTests
    {
        private readonly WebApplicationBuilder _sut;

        public ModuleInstallerTests()
        {
            _sut = WebApplication.CreateBuilder();
        }

        [Test]
        public void AddSQL_ConfigurationProvidedAsParameter_SQLServicesAreAddedToServiceCollection()
        {
            //Arrange
            SQLConfiguration sqlConfiguration = new SQLConfiguration
            {
                ConnectionString = "ExampleConnectionString"
            };

            //Act
            _sut.Services.AddSQL(sqlConfiguration);

            //Assert
            var app = _sut.Build();

            var sqlConnectionFactory = app.Services.GetService<ISQLConnectionFactory>();
            sqlConnectionFactory.Should().NotBeNull();
            sqlConnectionFactory!.GetConnectionString().Should().Be(sqlConfiguration.ConnectionString);
        }
    }
}
