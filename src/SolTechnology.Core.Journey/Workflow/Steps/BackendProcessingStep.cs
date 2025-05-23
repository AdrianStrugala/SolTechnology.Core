using SolTechnology.Core.Journey.Workflow.ChainFramework;
using SolTechnology.Core.Journey.Workflow.Handlers; // For SampleOrderContext
using System;
using System.Collections.Generic; // Not strictly needed for this simple version
using System.Threading.Tasks;

namespace SolTechnology.Core.Journey.Workflow.Steps
{
    public class BackendProcessingStep : IAutomatedChainStep<SampleOrderContext>
    {
        public string StepId => "ProcessOrderPayment";
        private readonly bool _forceFailForTest;


        public BackendProcessingStep(bool forceFailForTest = false) // Add default value
        {
            _forceFailForTest = forceFailForTest;
        }

        public Task<Result> Execute(SampleOrderContext context)
        {
            if (context.Input == null)
            {
                return Task.FromResult(Result.Failure("Input data is missing in context for payment processing."));
            }

            if (_forceFailForTest && context.Input.Quantity < 0) // Example condition for forced test failure
            {
                context.ErrorMessage = "Forced failure due to invalid quantity for processing.";
                return Task.FromResult(Result.Failure(context.ErrorMessage));
            }
            
            // Actual logic for failure based on quantity
            if (context.Input.Quantity < 0) { 
                context.ErrorMessage = "Invalid quantity for processing.";
                 return Task.FromResult(Result.Failure(context.ErrorMessage));
            }

            // Simulate payment processing
            context.ProcessedPaymentAmount = context.Input.Quantity * 10.5; // Example calculation
            context.IsInventoryChecked = true; // Simulate another part of backend work

            // Simulate a potential failure scenario based on OrderId (can be kept or removed if covered by above)
            if (context.Input.OrderId.Contains("FAIL_PAYMENT"))
            {
                context.ErrorMessage = "Simulated payment failure for OrderID: " + context.Input.OrderId;
                return Task.FromResult(Result.Failure(context.ErrorMessage));
            }

            return Task.FromResult(Result.Success());
        }
    }
}
