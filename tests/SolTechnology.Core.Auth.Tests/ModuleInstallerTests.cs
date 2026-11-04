using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace SolTechnology.Core.Auth.Tests
{
    [TestFixture]
    public class ModuleInstallerTests
    {
        [Test]
        public void AddSolAuth_WhenApiKeyMissing_ThrowsAtRegistration()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new AuthConfiguration();

            // Act
            var act = () => services.AddSolAuth(configuration);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage($"*{nameof(AuthConfiguration)}*is missing*");
        }

        [Test]
        public void AddSolAuth_WhenApiKeyProvided_ReturnsSameServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new AuthConfiguration { ApiKey = "secret" };

            // Act
            var result = services.AddSolAuth(configuration);

            // Assert
            result.Should().BeSameAs(services);
        }
    }
}
