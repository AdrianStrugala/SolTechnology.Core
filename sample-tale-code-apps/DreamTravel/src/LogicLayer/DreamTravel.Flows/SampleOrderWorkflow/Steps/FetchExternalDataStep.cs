using SolTechnology.Core.CQRS;
using SolTechnology.Core.Flow.Workflow.ChainFramework;

namespace DreamTravel.Flows.SampleOrderWorkflow.Steps
{
    public class FetchExternalDataStep : AutomatedFlowStep<SampleOrderContext>
    {
        public string StepId => "FetchShippingEstimate";
        
        public override async Task<Result> Execute(SampleOrderContext context)
        {
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
