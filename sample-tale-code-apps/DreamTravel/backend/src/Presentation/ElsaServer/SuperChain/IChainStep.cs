namespace ElsaServer.SuperChain;

public interface IChainStep<in TContext> where TContext : class
{
    Task<Result> Execute(TContext context);
}