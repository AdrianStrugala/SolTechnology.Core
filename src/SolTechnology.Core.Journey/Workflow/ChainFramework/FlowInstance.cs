using SolTechnology.Core.Journey.Models;

namespace SolTechnology.Core.Journey.Workflow.ChainFramework
{
    public class FlowInstance
    {
        public string FlowId { get; init; } = null!;
        public string FlowHandlerName { get; init; } = null!;
        
        public StepInfo? CurrentStep { get; set; }
        
        public List<StepInfo> History { get; set; } = new();
                
        public FlowStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        
        public object? Context { get; set; }
        

        public FlowInstance(string flowId, string flowHandlerName, object context)
        {
            FlowId = flowId;
            FlowHandlerName = flowHandlerName;
            CreatedAt = DateTime.UtcNow;
            LastUpdatedAt = DateTime.UtcNow;
            Status = FlowStatus.NotStarted;
            Context = context;
        }

        public FlowInstance()
        {
        }
    }

}
