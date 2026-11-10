using System.Net;
using System.Net.Http.Headers;
using DreamTravel.Api;
using FluentAssertions;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.API.Testing;

namespace DreamTravel.FunctionalTests;

/// <summary>
/// Boots the API in the Production environment and checks that an unhandled exception surfaces
/// as a generic ProblemDetails response, never as the developer exception page.
/// </summary>
public sealed class ProductionErrorHandlingTests
{
    private const string ThrowingPath = "/component-tests/throw";
    private const string ExceptionMessage = "Component test failure that must not leak";

    private APIFixture<Program> _productionApi = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var configuration = new TestConfigurationBuilder()
            .AddJsonFile("appsettings.tests.json")
            .Override("environment", "Production")
            .Override("Sql:ConnectionString", ComponentTestsFixture.SqlFixture.DatabaseConnectionString)
            .Override("HTTPClients:Google:BaseAddress", $"{ComponentTestsFixture.WireMockFixture.Url}/google/")
            .Build();

        _productionApi = new APIFixture<Program>(
            configuration,
            services =>
            {
                services.AddTransient<IStartupFilter, ThrowingEndpointStartupFilter>();
                // Hangfire's configuration is process-wide: reuse the shared API's storage, so this short-lived
                // host does not rebind Hangfire's logging to a logger factory it disposes on tear-down.
                services.AddSingleton(_ => JobStorage.Current);
            });
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _productionApi.Dispose();
    }

    [Test]
    public async Task UnhandledException_InProduction_ReturnsProblemDetailsWithoutDeveloperPage()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, ThrowingPath);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await _productionApi.ServerClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        body.Should().NotContain(ExceptionMessage);
        body.Should().NotContain(nameof(ThrowingEndpointStartupFilter));
    }

    // Appended after the app pipeline, so the exception passes back through its error handling.
    private sealed class ThrowingEndpointStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Map(ThrowingPath, branch => branch.Run(_ => throw new InvalidOperationException(ExceptionMessage)));
        };
    }
}
