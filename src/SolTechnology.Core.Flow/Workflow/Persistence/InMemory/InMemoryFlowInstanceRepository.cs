using System.Collections.Concurrent;
using SolTechnology.Core.Flow.Workflow.ChainFramework;
// For FlowInstance, IFlowInstanceRepository
// For thread-safe dictionary

namespace SolTechnology.Core.Flow.Workflow.Persistence.InMemory
{
    public class InMemoryFlowInstanceRepository : IFlowInstanceRepository
    {
        private readonly ConcurrentDictionary<string, FlowInstance> _flows = new ConcurrentDictionary<string, FlowInstance>();

        public Task<FlowInstance?> FindById(string flowId)
        {
            _flows.TryGetValue(flowId, out var flowInstance);
            // Return a clone to simulate behavior of a real repository (preventing direct modification of stored instance)
            return Task.FromResult<FlowInstance?>(Clone(flowInstance));
        }

        public Task SaveAsync(FlowInstance flowInstance)
        {
            if (flowInstance == null)
            {
                // Or throw new ArgumentNullException(nameof(flowInstance));
                return Task.CompletedTask; 
            }

            // Store a clone to prevent external modifications to the instance in the dictionary
            var instanceToStore = Clone(flowInstance);
            if(instanceToStore != null) // Clone method could return null
            {
                instanceToStore.LastUpdatedAt = System.DateTime.UtcNow; // Ensure LastUpdatedAt is fresh on save
                _flows[flowInstance.FlowId] = instanceToStore;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string flowId)
        {
            _flows.TryRemove(flowId, out _);
            return Task.CompletedTask;
        }

        // Simplified cloning method.
        private FlowInstance? Clone(FlowInstance? original)
        {
            if (original == null) return null;

            // This is a shallow copy. ContextData will be a reference copy.
            // For a true in-memory DB behavior preventing cross-test contamination or unintended modifications,
            // ContextData would also need to be deep cloned (e.g., via serialization/deserialization).
            // This is a known simplification for this in-memory repository.
            var clone = new FlowInstance
            {
                FlowId = original.FlowId,
                FlowHandlerName = original.FlowHandlerName,
                Context = original.Context, // Reference copy for ContextData
                CreatedAt = original.CreatedAt,
                LastUpdatedAt = original.LastUpdatedAt, // Will be updated in SaveAsync for the stored version
                Status = original.Status
            };
            return clone;
        }
    }
}
