using System.Runtime.CompilerServices;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.Journey.Workflow.ChainFramework
{
    public interface IFlowStep<TContext>
        where TContext : class
    {
        /// <summary>
        /// By default this returns the *runtime type*'s full name.
        /// Implementors can override by explicitly declaring StepId.
        /// </summary>
        public string StepId => GetType().FullName!;

        Task<Result> Execute(TContext context);
    }
}
