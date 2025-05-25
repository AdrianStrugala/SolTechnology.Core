using SolTechnology.Core.Journey.Workflow.ChainFramework;

namespace SolTechnology.Core.Journey.Workflow.Persistence
{
    public interface IJourneyInstanceRepository
    {
        Task<JourneyInstance?> GetByIdAsync(string journeyId);
        Task SaveAsync(JourneyInstance journeyInstance); // For create or update (upsert)
        Task DeleteAsync(string journeyId); // Optional for now, but good for completeness
    }
}
