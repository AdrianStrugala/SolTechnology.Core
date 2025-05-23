using System.Threading.Tasks;

namespace SolTechnology.Core.Journey.Workflow.ChainFramework
{
    public interface IChainStep<TContext>
        where TContext : class
    {
        Task<Result> Execute(TContext context);
    }
}
