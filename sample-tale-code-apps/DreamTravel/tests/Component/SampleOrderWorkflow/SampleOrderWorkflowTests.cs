using FluentAssertions;
using SolTechnology.Core.Tale.Api;
using SolTechnology.Core.Tale.Models;

namespace DreamTravel.FunctionalTests.SampleOrderWorkflow;

/// <summary>
/// Tests for SampleOrderWorkflow Tale with pause/resume functionality
/// </summary>
public class SampleOrderWorkflowTests
{
    private HttpClient _apiClient;

    [SetUp]
    public void Setup()
    {
        _apiClient = ComponentTestsFixture.ApiFixture.ServerClient;
    }

    [Test]
    public async Task HappyPath()
    {

        // Given: Initiate story request
        var createTaleResponse = await _apiClient
            .CreateRequest("/api/dreamtravel/tale/SampleOrderWorkflowTale/start")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithBody(new
            {
                OrderId = "2137",
                Quantity = 17
            })
            .PostAsync<TaleInstanceDto>();

        // Then: Tale is created and paused at interactive chapter
        createTaleResponse.Should().NotBeNull();
        createTaleResponse.Status.Should().Be(TaleStatus.WaitingForInput);
        createTaleResponse.CurrentChapter.Should().NotBeNull();
        createTaleResponse.CurrentChapter!.Status.Should().Be(TaleStatus.WaitingForInput);

        var storyId = createTaleResponse.TaleId;
        storyId.Should().NotBeNullOrEmpty();

        // When: Calling resume without user input
        var resumeWithoutInput = await _apiClient
            .CreateRequest($"/api/dreamtravel/tale/{storyId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .PostAsync<TaleInstanceDto>();

        // Then: Tale remains paused (no input provided for interactive chapter)
        resumeWithoutInput.Should().NotBeNull();
        resumeWithoutInput.Status.Should().Be(TaleStatus.WaitingForInput);

        // When: Calling resume with valid user input
        var resumeWithInput = await _apiClient
            .CreateRequest($"/api/dreamtravel/tale/{storyId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithBody(new
            {
                Name = "Adus",
                Address = "yes"
            })
            .PostAsync<TaleInstanceDto>();

        // Then: Tale completes successfully
        resumeWithInput.Should().NotBeNull();
        resumeWithInput.Status.Should().Be(TaleStatus.Completed);
        resumeWithInput.History.Should().HaveCountGreaterThan(0);

        // When: Getting story state
        var storyState = await _apiClient
            .CreateRequest($"/api/dreamtravel/tale/{storyId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .GetAsync<TaleInstanceDto>();

        // Then: Tale state shows completion
        storyState.Should().NotBeNull();
        storyState.Status.Should().Be(TaleStatus.Completed);
        storyState.TaleId.Should().Be(storyId);

        // When: Getting story result
        var storyResult = await _apiClient
            .CreateRequest($"/api/dreamtravel/tale/{storyId}/result")
            .WithHeader("X-API-KEY", "<SECRET>")
            .GetAsync<TaleInstanceDto>();

        // Then: Result is available
        storyResult.Should().NotBeNull();
        storyResult.Status.Should().Be(TaleStatus.Completed);
    }
}
