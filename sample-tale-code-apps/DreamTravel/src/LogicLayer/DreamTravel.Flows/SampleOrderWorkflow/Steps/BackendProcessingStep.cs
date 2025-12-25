using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.Flows.SampleOrderWorkflow.Chapters;

public class BackendProcessingChapter : Chapter<SampleOrderNarration>
{
    public override string ChapterId => "ProcessOrderPayment";

    public override Task<Result> Read(SampleOrderNarration narration)
    {
        // Example condition for forced test failure
        if (narration.Input.Quantity < 0)
        {
            var errorMessage = "Invalid quantity for processing.";
            return Task.FromResult(Result.Fail(errorMessage));
        }

        // Simulate payment processing
        narration.ProcessedPaymentAmount = narration.Input.Quantity * 10.5; // Example calculation
        narration.IsInventoryChecked = true; // Simulate another part of backend work

        // Simulate a potential failure scenario based on OrderId (can be kept or removed if covered by above)
        if (narration.Input.OrderId.Contains("FAIL_PAYMENT"))
        {
            var errorMessage = "Simulated payment failure for OrderID: " + narration.Input.OrderId;
            return Task.FromResult(Result.Fail(errorMessage));
        }

        return Result.SuccessAsTask();
    }
}