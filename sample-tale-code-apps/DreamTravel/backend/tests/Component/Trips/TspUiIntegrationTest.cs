using DreamTravel.Trips.Domain.Cities;
using DreamTravel.FunctionalTests.FakeApis;
using DreamTravel.Trips.GeolocationDataClients.GoogleApi;
using FluentAssertions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using SolTechnology.Core.Faker;

namespace DreamTravel.FunctionalTests.Trips;

[Parallelizable(ParallelScope.Self)]
public class TspUiIntegrationTest : PageTest
{
    private WireMockFixture _wireMockFixture = null!;
    private const string UiBaseUrl = "https://localhost:7024";
    private const string TspPageUrl = $"{UiBaseUrl}/tsp-map";

    [SetUp]
    public void Setup()
    {
        _wireMockFixture = ComponentTestsFixture.WireMockFixture;
    }

    [Test]
    public async Task TspFlow_AddTwoCitiesByName_CalculatesAndDisplaysResults()
    {
        // Arrange - Test cities
        var warsaw = new City
        {
            Name = "Warsaw",
            Latitude = 52.2297,
            Longitude = 21.0122,
            Country = "Poland"
        };

        var berlin = new City
        {
            Name = "Berlin",
            Latitude = 52.5200,
            Longitude = 13.4050,
            Country = "Germany"
        };

        var cities = new List<City> { warsaw, berlin };

        // Mock Google API - FindCityByName for both cities
        _wireMockFixture.Fake<IGoogleApiClient>()
            .WithRequest(x => x.GetLocationOfCity, warsaw.Name)
            .WithResponse(x => x
                .WithSuccess()
                .WithBody(GoogleFakeApi.BuildGeocodingResponse(warsaw)));

        _wireMockFixture.Fake<IGoogleApiClient>()
            .WithRequest(x => x.GetLocationOfCity, berlin.Name)
            .WithResponse(x => x
                .WithSuccess()
                .WithBody(GoogleFakeApi.BuildGeocodingResponse(berlin)));

        // Mock Google API - CalculateBestPath distance matrices
        _wireMockFixture.Fake<IGoogleApiClient>()
            .WithRequest(x => x.GetDurationMatrixByFreeRoad, cities)
            .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.FreeDistanceMatrix));

        _wireMockFixture.Fake<IGoogleApiClient>()
            .WithRequest(x => x.GetDurationMatrixByTollRoad, cities)
            .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.TollDistanceMatrix));

        // Act - Navigate to TSP page
        await Page.GotoAsync(TspPageUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Add first city (Warsaw)
        var firstCityInput = Page.Locator("input").First;
        await firstCityInput.FillAsync("Warsaw");
        await firstCityInput.PressAsync("Enter");

        // Wait for city to be resolved
        await Page.WaitForTimeoutAsync(1000);

        // Add second city (Berlin) - click "Add City" button first
        await Page.Locator("button:has-text('Add City')").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var secondCityInput = Page.Locator("input").Nth(1);
        await secondCityInput.FillAsync("Berlin");
        await secondCityInput.PressAsync("Enter");

        // Wait for city to be resolved
        await Page.WaitForTimeoutAsync(1000);

        // Run TSP calculation
        var runTspButton = Page.Locator("button:has-text('Run TSP')");
        await runTspButton.ClickAsync();

        // Assert - Wait for results to be displayed
        await Page.WaitForSelectorAsync("text=/Total [Tt]ime:/", new() { Timeout = 10000 });

        var resultsSection = Page.Locator("text=/Total [Tt]ime:/").First;
        await Expect(resultsSection).ToBeVisibleAsync();

        var costSection = Page.Locator("text=/Total [Cc]ost:/").First;
        await Expect(costSection).ToBeVisibleAsync();

        // Verify cost contains DKK
        var costText = await costSection.TextContentAsync();
        costText.Should().Contain("DKK");
    }

    [Test]
    public async Task TspFlow_AddCityByMapClick_ResolvesCoordinatesToCity()
    {
        // Arrange - Mock reverse geocoding (coordinates -> city name)
        var wroclaw = new City
        {
            Name = "Wroclaw",
            Latitude = 51.107883,
            Longitude = 17.038538,
            Country = "Poland"
        };

        // Mock GetNameOfCity - receives City with coords, returns City with name
        _wireMockFixture.Fake<IGoogleApiClient>()
            .WithRequest(x => x.GetNameOfCity, wroclaw)
            .WithResponse(x => x
                .WithSuccess()
                .WithBody(GoogleFakeApi.BuildGeocodingResponse(wroclaw)));

        // Act - Navigate to TSP page
        await Page.GotoAsync(TspPageUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click on map (simulating click near Wroclaw coordinates)
        // Note: This test assumes the map click will trigger reverse geocoding
        var mapElement = Page.Locator("#tsp-map");
        await mapElement.ClickAsync(new() { Position = new() { X = 300, Y = 200 } });

        // Wait for city to be resolved
        await Page.WaitForTimeoutAsync(2000);

        // Assert - Verify city name appears in input
        var cityInput = Page.Locator("input").Last;
        var cityName = await cityInput.InputValueAsync();
        cityName.Should().NotBeNullOrEmpty("city should be resolved from coordinates");
    }

    [Test]
    public async Task TspFlow_RunWithLessThanTwoCities_DisablesRunButton()
    {
        // Act - Navigate to TSP page
        await Page.GotoAsync(TspPageUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Run TSP button should be disabled (less than 2 cities)
        var runTspButton = Page.Locator("button:has-text('Run TSP')");
        await Expect(runTspButton).ToBeDisabledAsync();

        // Add one city but don't fill name
        await Page.Locator("button:has-text('Add City')").ClickAsync();

        // Button should still be disabled (cities without valid City data)
        await Expect(runTspButton).ToBeDisabledAsync();
    }
}
