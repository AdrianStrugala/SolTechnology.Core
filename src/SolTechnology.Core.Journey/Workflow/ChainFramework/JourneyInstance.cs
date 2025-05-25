using System;
using SolTechnology.Core.Journey.Workflow; // For FlowStatus

namespace SolTechnology.Core.Journey.Workflow.ChainFramework
{
    public class JourneyInstance
    {
        public string JourneyId { get; set; } = null!;
        public string FlowHandlerName { get; set; } = null!;
        public object ContextData { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public FlowStatus CurrentStatus { get; set; }

        public JourneyInstance()
        {
            // Default constructor for deserialization/mapping
        }

        public JourneyInstance(string journeyId, string flowHandlerName, object contextData)
        {
            JourneyId = journeyId;
            FlowHandlerName = flowHandlerName;
            ContextData = contextData;
            CreatedAt = DateTime.UtcNow;
            LastUpdatedAt = DateTime.UtcNow;

            // Assuming contextData is a ChainContext<TInput, TOutput> or similar
            // and has a Status property. This requires reflection or a common interface/base class.
            // For simplicity, if contextData is known to be TContext which has a Status property:
            if (contextData is IStatusProvider statusProvider) // Hypothetical interface
            {
                CurrentStatus = statusProvider.Status;
            }
            else if (contextData.GetType().GetProperty("Status")?.GetValue(contextData) is FlowStatus status)
            {
                 CurrentStatus = status;
            }
            else
            {
                CurrentStatus = FlowStatus.NotStarted; // Default if status cannot be determined
            }
        }
    }

    // Hypothetical interface to make status extraction cleaner from ChainContext
    // This would require ChainContext to implement it.
    public interface IStatusProvider
    {
        FlowStatus Status { get; }
    }
}
