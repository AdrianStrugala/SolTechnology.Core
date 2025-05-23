using SolTechnology.Core.Journey.Workflow.ChainFramework;
using SolTechnology.Core.Journey.Workflow.Handlers; // For SampleOrderContext
using System.Threading.Tasks;

namespace SolTechnology.Core.Journey.Workflow.Steps
{
    public class FetchExternalDataStep : IAutomatedChainStep<SampleOrderContext>
    {
        public string StepId => "FetchShippingEstimate";

        public FetchExternalDataStep() { } // If no specific DI needed for the step itself

        public async Task<Result> Execute(SampleOrderContext context)
        {
            if (context.Input == null)
            {
                return Result.Failure("Input data is missing in context for fetching shipping estimate.");
            }

            // Simulate an API call
            await Task.Delay(50); 

            // Set some value on the context's output
            // Note: SampleOrderContext inherits from ChainContext<SampleOrderInput, SampleOrderResult>
            // So, context.Output is of type SampleOrderResult
            context.Output.FinalMessage = $"Shipping estimate for Order {context.Input.OrderId}: 2 days";
            
            return Result.Success();
        }
    }
}
