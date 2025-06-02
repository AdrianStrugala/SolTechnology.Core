using DreamTravel.Flows.SampleOrderWorkflow.Steps;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Journey.Workflow.ChainFramework;
using SolTechnology.Core.Journey.Workflow.Handlers;
using SolTechnology.Core.Journey.Workflow.Steps;

namespace DreamTravel.Flows.SampleOrderWorkflow
{
    public class SampleOrderWorkflowHandler(
        IServiceProvider serviceProvider,
        ILogger<SampleOrderWorkflowHandler> logger)
        : PausableChainHandler<SampleOrderInput, SampleOrderContext, SampleOrderResult>(serviceProvider, logger)
    {
        protected override async Task HandleChainDefinition(SampleOrderContext context)
        {
            // The PausableChainHandler's ExecuteHandler will manage continuing or stopping based on these results.
            // Invoke should internally handle the "don't proceed if prior step failed/paused" logic.

            // Step 1: Request User Input for Customer Details
            await Invoke<RequestUserInputStep>();

            // Step 2: Process Payment (Automated)
            await Invoke<BackendProcessingStep>();

            // Step 3: Fetch Shipping Estimate (Automated)
            await Invoke<FetchExternalDataStep>();

            // If all steps complete successfully:
            context.Output.OrderId = context.Input.OrderId;
            context.Output.IsSuccessfullyProcessed = true;
            context.Output.Name = context.CustomerDetails?.Name;
            if (string.IsNullOrEmpty(context.Output.FinalMessage)) // Ensure FinalMessage has a value
            {
                context.Output.FinalMessage = "Order processed and shipping estimate obtained.";
            }
            // Status will be set to Completed by ExecuteHandler if HandleChainDefinition finishes.
        }
    }
}
