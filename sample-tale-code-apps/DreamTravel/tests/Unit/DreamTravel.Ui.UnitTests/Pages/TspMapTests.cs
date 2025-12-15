using Bunit;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Domain.Paths;
using DreamTravel.Ui.Models;
using DreamTravel.Ui.Pages;
using DreamTravel.Ui.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SolTechnology.Core.CQRS;
using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.Ui.UnitTests.Pages;

public class TspMapTests
{
    private Bunit.TestContext _testContext = null!;
    private ITspService _tspService = null!;
    private IJSRuntime _jsRuntime = null!;

    [SetUp]
    public void Setup()
    {
        // Create new TestContext for each test
        _testContext = new Bunit.TestContext();

        // Mock dependencies
        _tspService = Substitute.For<ITspService>();
        _jsRuntime = Substitute.For<IJSRuntime>();

        // Setup JSInterop
        _testContext.JSInterop.Mode = JSRuntimeMode.Loose;

        // Register services BEFORE any component rendering
        _testContext.Services.AddSingleton(_tspService);
        _testContext.Services.AddSingleton(_jsRuntime);
        _testContext.Services.AddSingleton(Substitute.For<ISnackbar>());
        _testContext.Services.AddSingleton(new DreamTravel.Ui.Configuration.GoogleMapsConfiguration { ApiKey = "test-key" });

        // Add MudBlazor stubs
        _testContext.ComponentFactories.AddStub<MudPopoverProvider>();
        _testContext.ComponentFactories.AddStub<MudDialogProvider>();
        _testContext.ComponentFactories.AddStub<MudSnackbarProvider>();

        // Stub child components (we're testing TspMap logic, not rendering)
        _testContext.ComponentFactories.AddStub<DreamTravel.Ui.Components.TripPlanner.CitiesPanel>();
        _testContext.ComponentFactories.AddStub<DreamTravel.Ui.Components.Shared.GoogleMap>();
    }

    [TearDown]
    public void TearDown()
    {
        _testContext?.Dispose();
    }

    [Test]
    public void TspMap_Renders_WithoutErrors()
    {
        // Act
        var cut = _testContext.RenderComponent<TspMap>();

        // Assert - Component renders without throwing
        cut.Should().NotBeNull();
        cut.Markup.Should().NotBeEmpty();
    }

}
