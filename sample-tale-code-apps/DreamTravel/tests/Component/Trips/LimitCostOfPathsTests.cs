using System.Net;
using System.Text.Json;
using FluentAssertions;
using SolTechnology.Core.HTTP;
using Path = DreamTravel.Domain.Paths.Path;

namespace DreamTravel.FunctionalTests.Trips;

public class LimitCostOfPathsTests
{
    private HttpClient _apiClient = null!;

    [SetUp]
    public void Setup()
    {
        _apiClient = ComponentTestsFixture.ApiFixture.ServerClient;
    }

    [Test]
    public async Task LimitCost_PathsOverTheLimit_FreesTheRoadsThatDoNotFit()
    {
        // Arrange
        var paths = new List<Path>
        {
            new() { Index = 0, Goal = 1, Cost = 10, TollDistance = 100, FreeDistance = 150 },
            new() { Index = 1, Goal = 2, Cost = 50, TollDistance = 200, FreeDistance = 260 }
        };

        // Act
        using var response = await LimitCost(new { CostLimit = 20, Paths = paths }).Post();
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var limited = JsonSerializer.Deserialize<List<Path>>(body, JsonSerializerOptions.Web)!;
        limited.Select(p => (p.Index, p.OptimalCost, p.OptimalDistance))
            .Should().Equal((0, 10d, 100d), (1, 0d, 260d));
    }

    [Test]
    public async Task LimitCost_NegativeLimitAndNoPaths_ReturnsValidationProblemDetails()
    {
        // Act
        using var response = await LimitCost(new { CostLimit = -1, Paths = new List<Path>() }).Post();
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonDocument.Parse(body).RootElement;
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("errors").EnumerateObject().Select(e => e.Name).Should().BeEquivalentTo("CostLimit", "Paths");
    }

    private RequestBuilder LimitCost(object body) => _apiClient
        .CreateRequest("/api/LimitCost")
        .WithHeader("X-API-KEY", "<SECRET>")
        .WithHeader("X-API-VERSION", "1.0")
        .WithBody(body);
}
