using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
            _sut.Services.AddSolSQL(sqlConfiguration);

            //Assert
            var app = _sut.Build();

            var sqlConnectionFactory = app.Services.GetService<ISQLConnectionFactory>();
            sqlConnectionFactory.Should().NotBeNull();
            sqlConnectionFactory!.GetConnectionString().Should().Be(sqlConfiguration.ConnectionString);
        }

        [TestCase("")]
        [TestCase("   ")]
        public async Task AddSolSQL_EmptyConnectionString_HostStartFails(string connectionString)
        {
            // Arrange
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSolSQL(new SQLConfiguration { ConnectionString = connectionString });
            using var host = builder.Build();

            // Act
            var start = () => host.StartAsync();

            // Assert
            await start.Should().ThrowAsync<OptionsValidationException>()
                .WithMessage("*ConnectionString*");
        }

        [Test]
        public void AddSolSQL_NullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var addSolSQL = () => services.AddSolSQL(null!);

            // Assert
            addSolSQL.Should().Throw<ArgumentNullException>().WithParameterName("sqlConfiguration");
        }
    }
}
