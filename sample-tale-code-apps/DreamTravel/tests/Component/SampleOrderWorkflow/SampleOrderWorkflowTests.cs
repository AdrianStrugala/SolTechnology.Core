using DreamTravel.Flows.SampleOrderWorkflow;
using FluentAssertions;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story.Api;
using SolTechnology.Core.Story.Models;

namespace DreamTravel.FunctionalTests.SampleOrderWorkflow;

/// <summary>
/// Tests for SampleOrderWorkflow Story with pause/resume functionality
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
        var createStoryResponse = await _apiClient
            .CreateRequest("/api/dreamtravel/story/SampleOrderWorkflowStory/start")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithBody(new
            {
                OrderId = "2137",
                Quantity = 17
            })
            .PostAsync<Result<StoryInstanceDto>>();

        // Then: Story is created and paused at interactive chapter
        createStoryResponse.Should().NotBeNull();
        createStoryResponse.IsSuccess.Should().BeTrue();
        createStoryResponse.Data.Should().NotBeNull();
        createStoryResponse.Data!.Status.Should().Be(StoryStatus.WaitingForInput);
        createStoryResponse.Data.CurrentChapter.Should().NotBeNull();
        createStoryResponse.Data.CurrentChapter!.Status.Should().Be(StoryStatus.WaitingForInput);

        var storyId = createStoryResponse.Data.StoryId;
        storyId.Should().NotBeNullOrEmpty();

        // When: Calling resume without user input
        var resumeWithoutInput = await _apiClient
            .CreateRequest($"/api/dreamtravel/story/{storyId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .PostAsync<Result<StoryInstanceDto>>();

        // Then: Story remains paused (no input provided for interactive chapter)
        resumeWithoutInput.Should().NotBeNull();
        resumeWithoutInput.Data.Should().NotBeNull();
        resumeWithoutInput.Data!.Status.Should().Be(StoryStatus.WaitingForInput);

        // When: Calling resume with valid user input
        var resumeWithInput = await _apiClient
            .CreateRequest($"/api/dreamtravel/story/{storyId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithBody(new
            {
                Name = "Adus",
                Address = "yes"
            })
            .PostAsync<Result<StoryInstanceDto>>();

        // Then: Story completes successfully
        resumeWithInput.Should().NotBeNull();
        resumeWithInput.IsSuccess.Should().BeTrue();
        resumeWithInput.Data.Should().NotBeNull();
        resumeWithInput.Data!.Status.Should().Be(StoryStatus.Completed);
        resumeWithInput.Data.History.Should().HaveCountGreaterThan(0);

        // When: Getting story state
        var storyState = await _apiClient
            .CreateRequest($"/api/dreamtravel/story/{storyId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .GetAsync<Result<StoryInstanceDto>>();

        // Then: Story state shows completion
        storyState.Should().NotBeNull();
        storyState.IsSuccess.Should().BeTrue();
        storyState.Data.Should().NotBeNull();
        storyState.Data!.Status.Should().Be(StoryStatus.Completed);
        storyState.Data.StoryId.Should().Be(storyId);

        // When: Getting story result
        var storyResult = await _apiClient
            .CreateRequest($"/api/dreamtravel/story/{storyId}/result")
            .WithHeader("X-API-KEY", "<SECRET>")
            .GetAsync<Result<StoryInstanceDto>>();

        // Then: Result is available
        storyResult.Should().NotBeNull();
        storyResult.IsSuccess.Should().BeTrue();
        storyResult.Data.Should().NotBeNull();
        storyResult.Data!.Status.Should().Be(StoryStatus.Completed);
    }
}