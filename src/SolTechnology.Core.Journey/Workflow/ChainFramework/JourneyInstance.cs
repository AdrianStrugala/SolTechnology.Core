using System;
using SolTechnology.Core.Journey.Workflow; // For FlowStatus

namespace SolTechnology.Core.Journey.Workflow.ChainFramework
{
    public class JourneyInstance
    {
        public string JourneyId { get; set; } = null!;
        public string FlowHandlerName { get; set; } = null!;
        
        public FlowStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
      
        public object? InputData { get; set; }
        
        public string? CurrentStep { get; set; }
        public List<StepInfo> History { get; set; } = new List<StepInfo>();
        public string? ErrorMessage { get; set; }

        public JourneyInstance()
        {
            // Default constructor for deserialization/mapping
        }

        public JourneyInstance(string journeyId, string flowHandlerName, object? input)
        {
            JourneyId = journeyId;
            FlowHandlerName = flowHandlerName;
            CreatedAt = DateTime.UtcNow;
            LastUpdatedAt = DateTime.UtcNow;
            Status = FlowStatus.NotStarted;
            InputData = input;
        }
    }

}
