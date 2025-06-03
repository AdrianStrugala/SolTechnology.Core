using SolTechnology.Core.Flow.Workflow.ChainFramework;

namespace SolTechnology.Core.Flow.Workflow.Persistence
{
    public interface IFlowInstanceRepository
    {
        Task<FlowInstance?> FindById(string flowId);
        Task SaveAsync(FlowInstance flowInstance); // For create or update (upsert)
        Task DeleteAsync(string flowId); // Optional for now, but good for completeness
    }
}
