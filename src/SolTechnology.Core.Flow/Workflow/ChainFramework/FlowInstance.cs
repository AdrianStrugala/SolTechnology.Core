using System.Text.Json.Serialization;
using SolTechnology.Core.Flow.Models;

namespace SolTechnology.Core.Flow.Workflow.ChainFramework
{
    public class FlowInstance
    {
        public string FlowId { get; init; } = null!;
        public string FlowHandlerName { get; init; } = null!;
        
        public StepInfo? CurrentStep { get; set; }
        
        // ReSharper disable once CollectionNeverQueried.Global
        public List<StepInfo> History { get; set; } = new();
                
        public FlowStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }

        [JsonIgnore]
        public dynamic? Context { get; set; }
        

        public FlowInstance(string flowId, string flowHandlerName, object context)
        {
            FlowId = flowId;
            FlowHandlerName = flowHandlerName;
            CreatedAt = DateTime.UtcNow;
            LastUpdatedAt = DateTime.UtcNow;
            Status = FlowStatus.Created;
            Context = context;
        }

        public FlowInstance()
        {
        }
    }

}
