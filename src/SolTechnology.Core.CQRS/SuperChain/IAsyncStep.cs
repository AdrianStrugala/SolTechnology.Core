using System.Threading.Tasks;
using SolTechnology.Core.CQRS;

public interface IAsyncStep<in TContext> where TContext : class
{
    Task<Result> Execute(TContext context);
}