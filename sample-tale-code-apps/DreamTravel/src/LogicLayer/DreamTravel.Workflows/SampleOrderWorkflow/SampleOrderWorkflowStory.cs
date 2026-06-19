using DreamTravel.Flows.SampleOrderWorkflow.Chapters;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Story;
using SolTechnology.Core.Story.Tale;

namespace DreamTravel.Flows.SampleOrderWorkflow
{
    public class SampleOrderWorkflowStory(
        IServiceProvider serviceProvider,
        ILogger<SampleOrderWorkflowStory> logger)
        : StoryHandler<SampleOrderInput, SampleOrderContext, SampleOrderResult>(serviceProvider, logger)
    {
        protected override Tale<SampleOrderResult> Tell() =>
            Open<CustomerDetailsChapter>()         // pauses for customer details
                .Read<BackendProcessingChapter>()  // automated payment
                .Read<FetchExternalDataChapter>()  // automated shipping estimate
                .Finale(ctx =>
                {
                    ctx.Output.OrderId = ctx.Input.OrderId;
                    ctx.Output.IsSuccessfullyProcessed = true;
                    ctx.Output.Name = ctx.CustomerDetails?.Name;
                    if (string.IsNullOrEmpty(ctx.Output.FinalMessage))
                    {
                        ctx.Output.FinalMessage = "Order processed and shipping estimate obtained.";
                    }

                    return ctx.Output;
                });
    }
}
