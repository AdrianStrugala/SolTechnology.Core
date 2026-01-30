using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.SQL.Connections;
using Xunit;

namespace SolTechnology.Core.SQL.Tests
{
    public class ModuleInstallerTests
    {
        private readonly WebApplicationBuilder _sut;

        public ModuleInstallerTests()
        {
            _sut = WebApplication.CreateBuilder();
        }

        [Fact]
        public void AddSQL_ConfigurationProvidedAsParameter_SQLServicesAreAddedToServiceCollection()
        {

            //Arrange
            SQLConfiguration sqlConfiguration = new SQLConfiguration
            {
                ConnectionString = "ExampleConnectionString"
            };


            //Act
            _sut.Services.AddSolSQL(sqlConfiguration);


            //Assert
            var app = _sut.Build();

            ISQLConnectionFactory? sqlConnectionFactory = app.Services.GetService<ISQLConnectionFactory>();
            Assert.NotNull(sqlConnectionFactory);
            Assert.Equal(sqlConfiguration.ConnectionString, sqlConnectionFactory.GetConnectionString());
        }
    }
}