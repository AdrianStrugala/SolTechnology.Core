﻿using FluentAssertions;
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
        // Post-pivot wire shape: Story endpoints (StoryController returns Result<StoryInstanceDto>
        // internally) are unwrapped by Core.Api's global ResultConversionFilter into raw DTOs on
        // the wire. Failure responses arrive as application/problem+json with non-2xx status,
        // surfaced by RestEase/PostAsync<T> as deserialization errors / non-success status codes.

        // Given: Initiate story request
        var createStoryResponse = await _apiClient
            .CreateRequest("/api/dreamtravel/story/SampleOrderWorkflowStory/start")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithBody(new
            {
                OrderId = "2137",
                Quantity = 17
            })
            .PostAsync<StoryInstanceDto>();

        // Then: Story is created and paused at interactive chapter
        createStoryResponse.Should().NotBeNull();
        createStoryResponse.Status.Should().Be(StoryStatus.WaitingForInput);
        createStoryResponse.CurrentChapter.Should().NotBeNull();
        createStoryResponse.CurrentChapter!.Status.Should().Be(StoryStatus.WaitingForInput);

        var storyId = createStoryResponse.StoryId;
        storyId.Should().NotBeNullOrEmpty();

        // When: Calling resume without user input
        var resumeWithoutInput = await _apiClient
            .CreateRequest($"/api/dreamtravel/story/{storyId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .PostAsync<StoryInstanceDto>();

        // Then: Story remains paused (no input provided for interactive chapter)
        resumeWithoutInput.Should().NotBeNull();
        resumeWithoutInput.Status.Should().Be(StoryStatus.WaitingForInput);

        // When: Calling resume with valid user input
        var resumeWithInput = await _apiClient
            .CreateRequest($"/api/dreamtravel/story/{storyId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithBody(new
            {
                Name = "Adus",
                Address = "yes"
            })
            .PostAsync<StoryInstanceDto>();

        // Then: Story completes successfully
        resumeWithInput.Should().NotBeNull();
        resumeWithInput.Status.Should().Be(StoryStatus.Completed);
        resumeWithInput.History.Should().HaveCountGreaterThan(0);

        // When: Getting story state
        var storyState = await _apiClient
            .CreateRequest($"/api/dreamtravel/story/{storyId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .GetAsync<StoryInstanceDto>();

        // Then: Story state shows completion
        storyState.Should().NotBeNull();
        storyState.Status.Should().Be(StoryStatus.Completed);
        storyState.StoryId.Should().Be(storyId);

        // When: Getting story result
        var storyResult = await _apiClient
            .CreateRequest($"/api/dreamtravel/story/{storyId}/result")
            .WithHeader("X-API-KEY", "<SECRET>")
            .GetAsync<StoryInstanceDto>();

        // Then: Result is available
        storyResult.Should().NotBeNull();
        storyResult.Status.Should().Be(StoryStatus.Completed);
    }
}
