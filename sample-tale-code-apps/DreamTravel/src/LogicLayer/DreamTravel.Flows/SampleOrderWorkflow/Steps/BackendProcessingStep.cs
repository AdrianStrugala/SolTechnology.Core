using SolTechnology.Core.CQRS;
using SolTechnology.Core.Flow.Workflow.ChainFramework;

namespace DreamTravel.Flows.SampleOrderWorkflow.Steps;

public class BackendProcessingStep : AutomatedFlowStep<SampleOrderContext>
{
    public string StepId => "ProcessOrderPayment";
    
    public override Task<Result> Execute(SampleOrderContext context)
    {
        // Example condition for forced test failure
        if (context.Input.Quantity < 0) { 
            var errorMessage = "Invalid quantity for processing.";
            return Task.FromResult(Result.Fail(errorMessage));
        }

        // Simulate payment processing
        context.ProcessedPaymentAmount = context.Input.Quantity * 10.5; // Example calculation
        context.IsInventoryChecked = true; // Simulate another part of backend work

        // Simulate a potential failure scenario based on OrderId (can be kept or removed if covered by above)
        if (context.Input.OrderId.Contains("FAIL_PAYMENT"))
        {
            var errorMessage  = "Simulated payment failure for OrderID: " + context.Input.OrderId;
            return Task.FromResult(Result.Fail(errorMessage));
        }

        return Result.SuccessAsTask();
    }
}