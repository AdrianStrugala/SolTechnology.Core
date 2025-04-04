using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Sql.Connections;
using Xunit;

namespace SolTechnology.Core.Sql.Tests
{
    public class ModuleInstallerTests
    {
        private readonly WebApplicationBuilder _sut;

        public ModuleInstallerTests()
        {
            _sut = WebApplication.CreateBuilder();
        }

        [Fact]
        public void AddSql_ConfigurationProvidedAsParameter_SqlServicesAreAddedToServiceCollection()
        {

            //Arrange 
            SqlConfiguration sqlConfiguration = new SqlConfiguration
            {
                ConnectionString = "ExampleConnectionString"
            };


            //Act
            _sut.Services.AddSql(sqlConfiguration);


            //Assert
            var app = _sut.Build();

            ISqlConnectionFactory? sqlConnectionFactory = app.Services.GetService<ISqlConnectionFactory>();
            Assert.NotNull(sqlConnectionFactory);
            Assert.Equal(sqlConfiguration.ConnectionString, sqlConnectionFactory.GetConnectionString());
        }
    }
}