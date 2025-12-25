using DreamTravel.Flows.SampleOrderWorkflow.Chapters;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Story;
using SolTechnology.Core.Journey.Workflow.Handlers;

namespace DreamTravel.Flows.SampleOrderWorkflow
{
    public class SampleOrderWorkflowHandler(
        IServiceProvider serviceProvider,
        ILogger<SampleOrderWorkflowHandler> logger)
        : StoryHandler<SampleOrderInput, SampleOrderNarration, SampleOrderResult>(serviceProvider, logger)
    {
        protected override async Task TellStory()
        {
            // Chapter 1: Request User Input for Customer Details
            await ReadChapter<RequestUserInputChapter>();

            // Chapter 2: Process Payment (Automated)
            await ReadChapter<BackendProcessingChapter>();

            // Chapter 3: Fetch Shipping Estimate (Automated)
            await ReadChapter<FetchExternalDataChapter>();

            // If all chapters complete successfully:
            Narration.Output.OrderId = Narration.Input.OrderId;
            Narration.Output.IsSuccessfullyProcessed = true;
            Narration.Output.Name = Narration.CustomerDetails?.Name;
            if (string.IsNullOrEmpty(Narration.Output.FinalMessage))
            {
                Narration.Output.FinalMessage = "Order processed and shipping estimate obtained.";
            }
        }
    }
}
