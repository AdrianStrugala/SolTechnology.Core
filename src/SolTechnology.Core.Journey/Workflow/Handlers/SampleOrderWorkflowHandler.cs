using Microsoft.Extensions.Logging;
using SolTechnology.Core.Journey.Workflow.ChainFramework;
using SolTechnology.Core.Journey.Workflow.Steps; // For example steps
using System;
using System.Threading.Tasks;

namespace SolTechnology.Core.Journey.Workflow.Handlers
{
    public class SampleOrderWorkflowHandler : PausableChainHandler<SampleOrderInput, SampleOrderContext, SampleOrderResult>
    {
        public SampleOrderWorkflowHandler(IServiceProvider serviceProvider, ILogger<SampleOrderWorkflowHandler> logger)
            : base(serviceProvider, logger)
        {
        }

        protected override async Task HandleChainDefinition(SampleOrderContext context)
        {
            // The PausableChainHandler's ExecuteHandler will manage continuing or stopping based on these results.
            // InvokeNextAsync should internally handle the "don't proceed if prior step failed/paused" logic.

            Result stepResult;

            // Step 1: Request User Input for Customer Details
            stepResult = await InvokeNextAsync<RequestUserInputStep>(context);
            if (!stepResult.IsSuccess || stepResult.IsPaused) return; // Stop if failed or paused

            // Step 2: Process Payment (Automated)
            stepResult = await InvokeNextAsync<BackendProcessingStep>(context);
            if (!stepResult.IsSuccess || stepResult.IsPaused) return; // Stop if failed (paused not expected for automated)

            // Step 3: Fetch Shipping Estimate (Automated)
            stepResult = await InvokeNextAsync<FetchExternalDataStep>(context);
            if (!stepResult.IsSuccess || stepResult.IsPaused) return; // Stop if failed

            // If all steps complete successfully:
            context.Output.OrderId = context.Input.OrderId;
            context.Output.IsSuccessfullyProcessed = true;
            if (string.IsNullOrEmpty(context.Output.FinalMessage)) // Ensure FinalMessage has a value
            {
                context.Output.FinalMessage = "Order processed and shipping estimate obtained.";
            }
            // Status will be set to Completed by ExecuteHandler if HandleChainDefinition finishes.
        }
    }
}
