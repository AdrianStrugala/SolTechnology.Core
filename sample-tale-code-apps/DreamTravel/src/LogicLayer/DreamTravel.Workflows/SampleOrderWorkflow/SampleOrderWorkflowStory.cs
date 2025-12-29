using DreamTravel.Flows.SampleOrderWorkflow.Chapters;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Story;

namespace DreamTravel.Flows.SampleOrderWorkflow
{
    public class SampleOrderWorkflowStory(
        IServiceProvider serviceProvider,
        ILogger<SampleOrderWorkflowStory> logger)
        : StoryHandler<SampleOrderInput, SampleOrderContext, SampleOrderResult>(serviceProvider, logger)
    {
        protected override async Task TellStory()
        {
            // Chapter 1: Request User Input for Customer Details
            await ReadChapter<CustomerDetailsChapter>();

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
