using DreamTravel.Flows.SampleOrderWorkflow;
using FluentAssertions;
using SolTechnology.Core.CQRS;

namespace DreamTravel.FunctionalTests.SampleOrderWorkflow;

/// <summary>
/// Temporarily disabled - Story REST API not yet implemented (Week 4)
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
    [Ignore("Story REST API not yet implemented - deferred to Week 4")]
    public async Task HappyPath()
    {
        // "Given is initiate flow request".x(() =>
        var createFlowResponse = await _apiClient
            .CreateRequest("/api/flow/SampleOrderWorkflowHandler/start")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithBody(new
            {
                OrderId = "2137",
                Quantity = 17
            })
            .PostAsync<Result<dynamic>>();
            
        createFlowResponse.IsSuccess.Should().BeTrue();
        createFlowResponse.Data.Should().NotBeNull();
        // TODO: Update assertions for Story API when implemented
        var flowId = "test-id";
            
        // "When calling post flow with empty body".x(() =>
        var progressFlow = await _apiClient
            .CreateRequest($"/api/flow/{flowId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .PostAsync<Result<dynamic>>();

        // "Then execution stops on user input".x(() =>
        // TODO: Update assertions for Story API when implemented

            
        // "When calling post flow with expected body".x(() =>
        progressFlow = await _apiClient
            .CreateRequest($"/api/flow/{flowId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithBody(new
            {
                Name = "Adus",
                Address = "yes"
            })
            .PostAsync<Result<dynamic>>();

        // "Then rest of the flow is executed".x(() =>
        // TODO: Update assertions for Story API when implemented
            
            
        // "When calling get flow result".x(() =>
        var flowResult = await _apiClient
            .CreateRequest($"/api/flow/{flowId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .GetAsync<Result<SampleOrderResult>>();
            
        // "Then flow result contains expected data".x(() =>
        // TODO: Update assertions for Story API when implemented
    }
}