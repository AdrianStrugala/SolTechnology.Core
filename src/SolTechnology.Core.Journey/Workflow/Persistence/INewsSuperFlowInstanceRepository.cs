using System.Threading.Tasks;
using SolTechnology.Core.Journey.Workflow;

namespace SolTechnology.Core.Journey.Workflow.Persistence
{
    public interface INewsSuperFlowInstanceRepository
    {
        Task<NewsSuperFlowInstance?> GetByIdAsync(string instanceId);
        Task SaveAsync(NewsSuperFlowInstance instance);
        Task DeleteAsync(string instanceId);
    }
}
