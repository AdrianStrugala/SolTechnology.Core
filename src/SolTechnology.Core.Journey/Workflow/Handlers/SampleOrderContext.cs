using SolTechnology.Core.Journey.Workflow.ChainFramework;

namespace SolTechnology.Core.Journey.Workflow.Handlers
{
    public class SampleOrderContext : ChainContext<SampleOrderInput, SampleOrderResult>
    {
        // Custom properties for this specific workflow context
        public string CustomerDetails { get; set; } = null!;
        public double ProcessedPaymentAmount { get; set; }
        public bool IsInventoryChecked { get; set; }

        public SampleOrderContext() : base() { } // Ensure base constructor is called if needed
    }
}
