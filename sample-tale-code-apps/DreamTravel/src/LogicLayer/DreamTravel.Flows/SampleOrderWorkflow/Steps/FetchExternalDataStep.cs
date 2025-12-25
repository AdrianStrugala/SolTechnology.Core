using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.Flows.SampleOrderWorkflow.Chapters
{
    public class FetchExternalDataChapter : Chapter<SampleOrderNarration>
    {
        public override string ChapterId => "FetchShippingEstimate";

        public override async Task<Result> Read(SampleOrderNarration narration)
        {
            // Simulate an API call
            await Task.Delay(50);

            // Set some value on the narration's output
            narration.Output.FinalMessage = $"Shipping estimate for Order {narration.Input.OrderId}: 2 days";

            return Result.Success();
        }
    }
}
