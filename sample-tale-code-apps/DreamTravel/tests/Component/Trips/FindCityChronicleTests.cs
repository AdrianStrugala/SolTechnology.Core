using DreamTravel.Domain.Cities;
using DreamTravel.FunctionalTests.FakeApis;
using DreamTravel.GeolocationDataClients.GoogleApi;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using SolTechnology.Core.HTTP;
using SolTechnology.Core.HTTP.Testing;
using SolTechnology.Core.Logging.Chronicles;

namespace DreamTravel.FunctionalTests.Trips;

public class FindCityChronicleTests
{
    private HttpClient _apiClient = null!;
    private IServiceProvider _apiServices = null!;
    private WireMockFixture _wireMockFixture = null!;

    [SetUp]
    public void Setup()
    {
        _apiClient = ComponentTestsFixture.ApiFixture.ServerClient;
        _apiServices = ComponentTestsFixture.ApiFixture.TestServer.Services;
        _wireMockFixture = ComponentTestsFixture.WireMockFixture;
    }

    [Test]
    public async Task FindCityByName_OneRequest_EmitsOne2155EntryWithNestedCQRSAndHTTPSteps()
    {
        // Arrange
        var city = new City { Name = UniqueCityName(), Latitude = 50.06, Longitude = 19.94, Country = "Poland" };
        _wireMockFixture.Fake<IGoogleHTTPClient>()
            .WithRequest(x => x.GetLocationOfCity(city.Name))
            .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.BuildGeocodingResponse(city)));

        // Act
        await FindCity(city.Name).Post<City>();

        // Assert
        var report = RequestReports(city.Name).Should().ContainSingle().Which;
        TestContext.Out.WriteLine(report.Message);
        report.Level.Should().Be(LogLevel.Information);
        report.Message.Should().StartWith("Finished request [POST] [/api/FindCityByName] -> [200] in [")
            .And.Contain("Chronicle: [Ok]").And.NotContain("Event CitySearched");
        var query = Subtree(report.Message, "FindCityByNameQuery");
        query[0].Should().Contain($"[ok] Name={city.Name}");
        var fetchCity = Subtree(report.Message, "FetchCity ");
        fetchCity.Should().Contain(line => line.Contains("SQL SELECT City [")).And.Contain(line => line.Contains("HTTP GET Google"));
        query.Should().Contain(fetchCity[0]);
    }

    [Test]
    public async Task FindCityByName_UnknownCity_ReportShowsFailedStep()
    {
        // Arrange
        var name = UniqueCityName();
        _wireMockFixture.Fake<IGoogleHTTPClient>()
            .WithRequest(x => x.GetLocationOfCity(name))
            .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.ZeroResultsResponse));

        // Act
        await FindCity(name).TryPost<City>();

        // Assert
        var report = RequestReports(name).Should().ContainSingle().Which;
        TestContext.Out.WriteLine(report.Message);
        report.Level.Should().Be(LogLevel.Warning);
        report.Message.Should().StartWith("Finished request [POST] [/api/FindCityByName] -> [404] in [");
        Subtree(report.Message, "FindCityByNameQuery")[0].Should()
            .Contain($"[failed] Error=[NotFoundError: City [{name}] was not found]");
    }

    [Test]
    public void ChronicleOptions_BoundFromAppsettings_DespiteInstallerDefaults()
    {
        // Act
        var options = _apiServices.GetRequiredService<IOptions<ChronicleOptions>>().Value;

        // Assert
        options.SlowThreshold.Should().Be(TimeSpan.FromSeconds(2));
        options.CaptureSQLCommandText.Should().BeFalse();
    }

    private RequestBuilder FindCity(string name) => _apiClient
        .CreateRequest("/api/FindCityByName")
        .WithHeader("X-API-KEY", _apiServices.GetRequiredService<IConfiguration>()["Authentication:ApiKey"]!)
        .WithHeader("X-API-VERSION", "2.0")
        .WithBody(new { Name = name });

    // The collector is shared by every API test, so a report is found by its unique query attribute.
    private IReadOnlyList<FakeLogRecord> RequestReports(string cityName) =>
        [.. _apiServices.GetFakeLogCollector().GetSnapshot().Where(r => r.Id.Id == 2155 && r.Message.Contains($"Name={cityName}"))];

    private static string UniqueCityName() => "Chronicle" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>The line of the first step whose name starts with <paramref name="stepName"/>, followed by its descendants.</summary>
    private static List<string> Subtree(string message, string stepName)
    {
        var lines = message.Split('\n');
        var start = Array.FindIndex(lines, line => NameStart(line) is var at && at >= 0 && line[at..].StartsWith(stepName));
        start.Should().BeGreaterThanOrEqualTo(0, $"the report has a [{stepName}] step");
        var depth = NameStart(lines[start]);
        return [lines[start], .. lines.Skip(start + 1).TakeWhile(line => NameStart(line) > depth)];
    }

    private static int NameStart(string line)
    {
        var branch = Math.Max(line.IndexOf("|- ", StringComparison.Ordinal), line.IndexOf("`- ", StringComparison.Ordinal));
        return branch < 0 ? -1 : branch + 3;
    }
}
