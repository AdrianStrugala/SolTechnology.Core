using SolTechnology.Core.Journey.Workflow.ChainFramework;

namespace SolTechnology.Core.Journey.Workflow.Persistence
{
    public interface IJourneyInstanceRepository
    {
        Task<FlowInstance?> FindById(string journeyId);
        Task SaveAsync(FlowInstance flowInstance); // For create or update (upsert)
        Task DeleteAsync(string journeyId); // Optional for now, but good for completeness
    }
}
