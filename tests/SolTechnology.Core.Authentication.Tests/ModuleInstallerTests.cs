using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace SolTechnology.Core.Authentication.Tests
{
    [TestFixture]
    public class ModuleInstallerTests
    {
        [Test]
        public void AddSolAuthentication_WhenApiKeyMissing_ThrowsAtRegistration()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new AuthenticationConfiguration();

            // Act
            var act = () => services.AddSolAuthentication(configuration);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage($"*{nameof(AuthenticationConfiguration)}*is missing*");
        }

        [Test]
        public void AddSolAuthentication_WhenApiKeyProvided_ReturnsSameServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new AuthenticationConfiguration { ApiKey = "secret" };

            // Act
            var result = services.AddSolAuthentication(configuration);

            // Assert
            result.Should().BeSameAs(services);
        }
    }
}
