using DreamTravel.Flows.SampleOrderWorkflow.Chapters;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Story;
using SolTechnology.Core.Journey.Workflow.Handlers;

namespace DreamTravel.Flows.SampleOrderWorkflow
{
    public class SampleOrderWorkflowHandler : StoryHandler<SampleOrderInput, SampleOrderContext, SampleOrderResult>
    {
        public SampleOrderWorkflowHandler(
            IServiceProvider serviceProvider,
            ILogger<SampleOrderWorkflowHandler> logger)
            : base(serviceProvider, logger)
        {
        }
        protected override async Task TellStory()
        {
            // Chapter 1: Request User Input for Customer Details
            await ReadChapter<RequestUserInputChapter>();

            // Chapter 2: Process Payment (Automated)
            await ReadChapter<BackendProcessingChapter>();

            // Chapter 3: Fetch Shipping Estimate (Automated)
            await ReadChapter<FetchExternalDataChapter>();

            // If all chapters complete successfully:
            Context.Output.OrderId = Context.Input.OrderId;
            Context.Output.IsSuccessfullyProcessed = true;
            Context.Output.Name = Context.CustomerDetails?.Name;
            if (string.IsNullOrEmpty(Context.Output.FinalMessage))
            {
                Context.Output.FinalMessage = "Order processed and shipping estimate obtained.";
            }
        }
    }
}
