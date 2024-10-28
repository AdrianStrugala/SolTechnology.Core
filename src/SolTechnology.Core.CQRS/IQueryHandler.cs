using MediatR;

namespace SolTechnology.Core.CQRS
{
    public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, Result<TResult>> where TQuery : IRequest<Result<TResult>>
    {
    }
}