namespace SolTechnology.Core.CQRS.SuperChain;

public interface IChainStep<in TContext> where TContext : class
{
    Task<Result> Execute(TContext context);
}