using DreamTravel.Flows.SampleOrderWorkflow;
using FluentAssertions;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Flow.Models;
using SolTechnology.Core.Flow.Workflow.ChainFramework;

namespace DreamTravel.FunctionalTests.SampleOrderWorkflow;

public class SampleOrderWorkflowTests
{
    private HttpClient _apiClient;

    [SetUp]
    public void Setup()
    {
        _apiClient = IntegrationTestsFixture.ApiFixture.ServerClient;
    }

    [Test]
    public async Task HappyPath()
    {
        // "Given is initiate flow request".x(() =>
        var createFlowResponse = await _apiClient
            .CreateRequest("/api/journey/SampleOrderWorkflowHandler/start")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithBody(new
            {
                OrderId = "2137",
                Quantity = 17
            })
            .PostAsync<Result<FlowInstance>>();
            
        createFlowResponse.IsSuccess.Should().BeTrue();
        createFlowResponse.Data.Should().NotBeNull();
        createFlowResponse.Data!.FlowHandlerName.Should().Contain("DreamTravel.Flows.SampleOrderWorkflow.SampleOrderWorkflowHandler");
        createFlowResponse.Data!.Status.Should().Be(FlowStatus.Created);
        createFlowResponse.Data!.CreatedAt.Should().NotBe(default);
        createFlowResponse.Data.History.Should().BeEmpty();

        var flowId = createFlowResponse.Data.FlowId;
            
        // "When calling post flow with empty body".x(() =>
        var progressFlow = await _apiClient
            .CreateRequest($"/api/journey/{flowId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .PostAsync<Result<FlowInstance>>();
            
        // "Then execution stops on user input".x(() =>
        progressFlow.IsSuccess.Should().BeTrue();
        progressFlow.Data.Should().NotBeNull();
        progressFlow.Data!.CurrentStep.Should().NotBeNull();
        progressFlow.Data.CurrentStep!.Status = FlowStatus.WaitingForInput;
        progressFlow.Data.CurrentStep!.StepId = "RequestCustomerDetails";
        createFlowResponse.Data.History.Should().BeEmpty();

            
        // "When calling post flow with expected body".x(() =>
        progressFlow = await _apiClient
            .CreateRequest($"/api/journey/{flowId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithBody(new
            {
                Name = "Adus",
                Address = "yes"
            })
            .PostAsync<Result<FlowInstance>>();
            
        // "Then rest of the flow is executed".x(() =>
        progressFlow.IsSuccess.Should().BeTrue();
        progressFlow.Data.Should().NotBeNull();
        progressFlow.Data!.CurrentStep.Should().BeNull();
        progressFlow.Data.History.Should().HaveCount(3);
        progressFlow.Data!.Status.Should().Be(FlowStatus.Completed);
            
            
        // "When calling get flow result".x(() =>
        var flowResult = await _apiClient
            .CreateRequest($"/api/journey/{flowId}")
            .WithHeader("X-API-KEY", "<SECRET>")
            .GetAsync<Result<SampleOrderResult>>();
            
        // "Then flow result contains expected data".x(() =>
        flowResult.IsSuccess.Should().BeTrue();
        flowResult.Data.Should().NotBeNull();
        flowResult.Data!.OrderId = "2137";
        flowResult.Data!.Name = "Adus";
    }
}