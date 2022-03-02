using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SolTechnology.Core.Sql.Tests
{
    public class ModuleInstallerTests
    {
        [Fact]
        public void AddSql_ConfigurationProvidedAsParameter_SqlServivesAreAddedToServiceCollection()
        {

            //Arrange 
            SqlConfiguration sqlConfiguration = new SqlConfiguration();
            sqlConfiguration.ConnectionString = "ExampleConnectionString";

            var services = new ServiceCollection();


            //Act
            services.AddSql(sqlConfiguration);


            //Assert
            var x = services;
            //TO FINISH
        }
    }
}