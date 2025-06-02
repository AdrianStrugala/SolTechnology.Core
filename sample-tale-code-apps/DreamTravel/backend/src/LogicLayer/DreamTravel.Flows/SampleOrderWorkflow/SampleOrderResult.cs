namespace DreamTravel.Flows.SampleOrderWorkflow
{
    public class SampleOrderResult
    {
        public string OrderId { get; set; } = null!;
        public bool IsSuccessfullyProcessed { get; set; }
        public string? FinalMessage { get; set; }
        public string Name { get; set; } = null!;
    }
}
