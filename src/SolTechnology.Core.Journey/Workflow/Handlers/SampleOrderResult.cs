namespace SolTechnology.Core.Journey.Workflow.Handlers
{
    public class SampleOrderResult
    {
        public string OrderId { get; set; } = null!;
        public bool IsSuccessfullyProcessed { get; set; }
        public string? FinalMessage { get; set; }
    }
}
