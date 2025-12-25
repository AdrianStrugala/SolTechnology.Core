using SolTechnology.Core.Story;
using SolTechnology.Core.Journey.Workflow.Handlers;

namespace DreamTravel.Flows.SampleOrderWorkflow
{
    public class SampleOrderNarration : Narration<SampleOrderInput, SampleOrderResult>
    {
        // Custom properties for this specific workflow context
        public CustomerDetails CustomerDetails { get; set; } = null!;
        public double ProcessedPaymentAmount { get; set; }
        public bool IsInventoryChecked { get; set; }
    }

    public class CustomerDetails
    {
        public string? Name { get; set; } = null!;
        public string? Address { get; set; } = null!;
    }
}
