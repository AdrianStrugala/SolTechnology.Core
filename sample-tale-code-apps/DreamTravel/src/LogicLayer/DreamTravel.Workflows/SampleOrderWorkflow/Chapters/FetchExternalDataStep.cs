using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.Flows.SampleOrderWorkflow.Chapters
{
    public class FetchExternalDataChapter : Chapter<SampleOrderContext>
    {
        public override string ChapterId => "FetchShippingEstimate";

        public override async Task<Result> Read(SampleOrderContext context)
        {
            // Simulate an API call
            await Task.Delay(50);

            // Set some value on the context's output
            context.Output.FinalMessage = $"Shipping estimate for Order {context.Input.OrderId}: 2 days";

            return Result.Success();
        }
    }
}
