using Bunit;
using DreamTravel.Domain.Cities;
using DreamTravel.Ui.Components.TripPlanner;
using DreamTravel.Ui.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace DreamTravel.Ui.UnitTests.Components;

public class CitiesPanelTests : Bunit.TestContext
{
    [SetUp]
    public void Setup()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Stub MudBlazor components that require services
        ComponentFactories.AddStub<MudPopoverProvider>();
        ComponentFactories.AddStub<MudDialogProvider>();
        ComponentFactories.AddStub<MudSnackbarProvider>();
        ComponentFactories.AddStub<MudTextField<string>>();
    }

    [Test]
    public void CitiesPanel_RunTspButton_DisabledWhenNoCities()
    {
        // Arrange
        var cities = new List<CityEntry>();

        // Act
        var cut = RenderComponent<CitiesPanel>(parameters => parameters
            .Add(p => p.Cities, cities)
            .Add(p => p.IsLoading, false)
            .Add(p => p.TotalTime, "")
            .Add(p => p.TotalCost, 0));

        // Assert
        var runButton = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Calculate Route"));

        runButton.Should().NotBeNull();
        runButton!.HasAttribute("disabled").Should().BeTrue();
    }

    [Test]
    public void CitiesPanel_RunTspButton_DisabledWhenOnlyOneCityWithData()
    {
        // Arrange
        var cities = new List<CityEntry>
        {
            new()
            {
                Index = 0,
                Name = "Warsaw",
                City = new City { Name = "Warsaw", Latitude = 52.2297, Longitude = 21.0122, Country = "Poland" }
            }
        };

        // Act
        var cut = RenderComponent<CitiesPanel>(parameters => parameters
            .Add(p => p.Cities, cities)
            .Add(p => p.IsLoading, false)
            .Add(p => p.TotalTime, "")
            .Add(p => p.TotalCost, 0));

        // Assert
        var runButton = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Calculate Route"));

        runButton.Should().NotBeNull();
        runButton!.HasAttribute("disabled").Should().BeTrue();
    }

    [Test]
    public void CitiesPanel_RunTspButton_EnabledWhenTwoOrMoreCitiesWithData()
    {
        // Arrange
        var cities = new List<CityEntry>
        {
            new()
            {
                Index = 0,
                Name = "Warsaw",
                City = new City { Name = "Warsaw", Latitude = 52.2297, Longitude = 21.0122, Country = "Poland" }
            },
            new()
            {
                Index = 1,
                Name = "Berlin",
                City = new City { Name = "Berlin", Latitude = 52.5200, Longitude = 13.4050, Country = "Germany" }
            }
        };

        // Act
        var cut = RenderComponent<CitiesPanel>(parameters => parameters
            .Add(p => p.Cities, cities)
            .Add(p => p.IsLoading, false)
            .Add(p => p.TotalTime, "")
            .Add(p => p.TotalCost, 0));

        // Assert
        var runButton = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Calculate Route"));

        runButton.Should().NotBeNull();
        runButton!.HasAttribute("disabled").Should().BeFalse();
    }

    [Test]
    public void CitiesPanel_RunTspButton_DisabledWhenLoading()
    {
        // Arrange
        var cities = new List<CityEntry>
        {
            new()
            {
                Index = 0,
                Name = "Warsaw",
                City = new City { Name = "Warsaw", Latitude = 52.2297, Longitude = 21.0122, Country = "Poland" }
            },
            new()
            {
                Index = 1,
                Name = "Berlin",
                City = new City { Name = "Berlin", Latitude = 52.5200, Longitude = 13.4050, Country = "Germany" }
            }
        };

        // Act
        var cut = RenderComponent<CitiesPanel>(parameters => parameters
            .Add(p => p.Cities, cities)
            .Add(p => p.IsLoading, true)
            .Add(p => p.TotalTime, "")
            .Add(p => p.TotalCost, 0));

        // Assert
        var runButton = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Calculating") || b.TextContent.Contains("Calculate Route"));

        runButton.Should().NotBeNull();
        runButton!.HasAttribute("disabled").Should().BeTrue();
    }

    [Test]
    public void CitiesPanel_RunTspButton_ShowsCalculatingWhenLoading()
    {
        // Arrange
        var cities = new List<CityEntry>
        {
            new()
            {
                Index = 0,
                Name = "Warsaw",
                City = new City { Name = "Warsaw", Latitude = 52.2297, Longitude = 21.0122, Country = "Poland" }
            },
            new()
            {
                Index = 1,
                Name = "Berlin",
                City = new City { Name = "Berlin", Latitude = 52.5200, Longitude = 13.4050, Country = "Germany" }
            }
        };

        // Act
        var cut = RenderComponent<CitiesPanel>(parameters => parameters
            .Add(p => p.Cities, cities)
            .Add(p => p.IsLoading, true)
            .Add(p => p.TotalTime, "")
            .Add(p => p.TotalCost, 0));

        // Assert
        cut.Markup.Should().Contain("Calculating");
    }

    [Test]
    public void CitiesPanel_AddCityButton_InvokesCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var cities = new List<CityEntry>();

        // Act
        var cut = RenderComponent<CitiesPanel>(parameters => parameters
            .Add(p => p.Cities, cities)
            .Add(p => p.IsLoading, false)
            .Add(p => p.TotalTime, "")
            .Add(p => p.TotalCost, 0)
            .Add(p => p.OnCityAdded, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        var addButton = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Add City"));

        addButton.Should().NotBeNull();
        addButton!.Click();

        // Assert
        callbackInvoked.Should().BeTrue();
    }

    [Test]
    public void CitiesPanel_RemoveCityButton_InvokesCallbackWithCorrectIndex()
    {
        // Arrange
        var removedIndex = -1;
        var cities = new List<CityEntry>
        {
            new() { Index = 0, Name = "Warsaw" },
            new() { Index = 1, Name = "Berlin" }
        };

        // Act
        var cut = RenderComponent<CitiesPanel>(parameters => parameters
            .Add(p => p.Cities, cities)
            .Add(p => p.IsLoading, false)
            .Add(p => p.TotalTime, "")
            .Add(p => p.TotalCost, 0)
            .Add(p => p.OnCityRemoved, EventCallback.Factory.Create<int>(this, index => removedIndex = index)));

        // Find first remove button (close icon)
        var removeButtons = cut.FindAll("button")
            .Where(b => b.OuterHtml.Contains("Close") || b.OuterHtml.Contains("mud-icon-button"))
            .ToList();

        removeButtons.Should().NotBeEmpty();
        removeButtons.First().Click();

        // Assert
        removedIndex.Should().Be(0);
    }

    [Test]
    public void CitiesPanel_RunTspButton_InvokesCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var cities = new List<CityEntry>
        {
            new()
            {
                Index = 0,
                Name = "Warsaw",
                City = new City { Name = "Warsaw", Latitude = 52.2297, Longitude = 21.0122, Country = "Poland" }
            },
            new()
            {
                Index = 1,
                Name = "Berlin",
                City = new City { Name = "Berlin", Latitude = 52.5200, Longitude = 13.4050, Country = "Germany" }
            }
        };

        // Act
        var cut = RenderComponent<CitiesPanel>(parameters => parameters
            .Add(p => p.Cities, cities)
            .Add(p => p.IsLoading, false)
            .Add(p => p.TotalTime, "")
            .Add(p => p.TotalCost, 0)
            .Add(p => p.OnRunTsp, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        var runButton = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Calculate Route"));

        runButton.Should().NotBeNull();
        runButton!.Click();

        // Assert
        callbackInvoked.Should().BeTrue();
    }

    [Test]
    public void CitiesPanel_ResultsSection_HiddenWhenNoResults()
    {
        // Arrange
        var cities = new List<CityEntry>();

        // Act
        var cut = RenderComponent<CitiesPanel>(parameters => parameters
            .Add(p => p.Cities, cities)
            .Add(p => p.IsLoading, false)
            .Add(p => p.TotalTime, "")
            .Add(p => p.TotalCost, 0));

        // Assert - results section shouldn't be rendered at all when TotalTime is empty
        cut.Markup.Should().NotContain("DKK");
    }

    [Test]
    public void CitiesPanel_ResultsSection_DisplayedWhenResultsAvailable()
    {
        // Arrange
        var cities = new List<CityEntry>
        {
            new()
            {
                Index = 0,
                Name = "Warsaw",
                City = new City { Name = "Warsaw", Latitude = 52.2297, Longitude = 21.0122, Country = "Poland" }
            },
            new()
            {
                Index = 1,
                Name = "Berlin",
                City = new City { Name = "Berlin", Latitude = 52.5200, Longitude = 13.4050, Country = "Germany" }
            }
        };

        // Act
        var cut = RenderComponent<CitiesPanel>(parameters => parameters
            .Add(p => p.Cities, cities)
            .Add(p => p.IsLoading, false)
            .Add(p => p.TotalTime, "2:30:00")
            .Add(p => p.TotalCost, 111.9));

        // Assert - verify time and cost are displayed (no labels anymore, just values with icons)
        cut.Markup.Should().Contain("2:30:00");
        cut.Markup.Should().Contain("DKK");
        // Accept either locale format: 111.90 or 111,90
        cut.Markup.Should().Match(m => m.Contains("111.90") || m.Contains("111,90"));
    }
}
