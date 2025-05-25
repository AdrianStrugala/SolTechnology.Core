using System.Collections.Concurrent;
using SolTechnology.Core.Journey.Workflow.ChainFramework;
// For JourneyInstance, IJourneyInstanceRepository
// For thread-safe dictionary

namespace SolTechnology.Core.Journey.Workflow.Persistence.InMemory
{
    public class InMemoryJourneyInstanceRepository : IJourneyInstanceRepository
    {
        private readonly ConcurrentDictionary<string, JourneyInstance> _journeys = new ConcurrentDictionary<string, JourneyInstance>();

        public Task<JourneyInstance?> GetByIdAsync(string journeyId)
        {
            _journeys.TryGetValue(journeyId, out var journeyInstance);
            // Return a clone to simulate behavior of a real repository (preventing direct modification of stored instance)
            return Task.FromResult<JourneyInstance?>(Clone(journeyInstance)); 
        }

        public Task SaveAsync(JourneyInstance journeyInstance)
        {
            if (journeyInstance == null)
            {
                // Or throw new ArgumentNullException(nameof(journeyInstance));
                return Task.CompletedTask; 
            }

            // Store a clone to prevent external modifications to the instance in the dictionary
            var instanceToStore = Clone(journeyInstance);
            if(instanceToStore != null) // Clone method could return null
            {
                instanceToStore.LastUpdatedAt = System.DateTime.UtcNow; // Ensure LastUpdatedAt is fresh on save
                _journeys[journeyInstance.JourneyId] = instanceToStore;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string journeyId)
        {
            _journeys.TryRemove(journeyId, out _);
            return Task.CompletedTask;
        }

        // Simplified cloning method.
        private JourneyInstance? Clone(JourneyInstance? original)
        {
            if (original == null) return null;

            // This is a shallow copy. ContextData will be a reference copy.
            // For a true in-memory DB behavior preventing cross-test contamination or unintended modifications,
            // ContextData would also need to be deep cloned (e.g., via serialization/deserialization).
            // This is a known simplification for this in-memory repository.
            var clone = new JourneyInstance
            {
                JourneyId = original.JourneyId,
                FlowHandlerName = original.FlowHandlerName,
                ContextData = original.ContextData, // Reference copy for ContextData
                CreatedAt = original.CreatedAt,
                LastUpdatedAt = original.LastUpdatedAt, // Will be updated in SaveAsync for the stored version
                CurrentStatus = original.CurrentStatus
            };
            return clone;
        }
    }
}
