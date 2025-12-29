using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.Flows.SampleOrderWorkflow.Chapters;

public class BackendProcessingChapter : Chapter<SampleOrderContext>
{
    public override string ChapterId => "ProcessOrderPayment";

    public override Task<Result> Read(SampleOrderContext context)
    {
        // Example condition for forced test failure
        if (context.Input.Quantity < 0)
        {
            var errorMessage = "Invalid quantity for processing.";
            return Task.FromResult(Result.Fail(errorMessage));
        }

        // Simulate payment processing
        context.ProcessedPaymentAmount = context.Input.Quantity * 10.5; // Example calculation
        context.IsInventoryChecked = true; // Simulate another part of backend work

        // Simulate a potential failure scenario based on OrderId (can be kept or removed if covered by above)
        if (context.Input.OrderId.Contains("FAIL_PAYMENT"))
        {
            var errorMessage = "Simulated payment failure for OrderID: " + context.Input.OrderId;
            return Task.FromResult(Result.Fail(errorMessage));
        }

        return Result.SuccessAsTask();
    }
}