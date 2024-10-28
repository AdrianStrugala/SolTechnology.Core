using MediatR;

namespace SolTechnology.Core.CQRS
{
    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result> where TCommand : IRequest<Result>
    {
    }

    public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, Result<TResult>> where TCommand : IRequest<Result<TResult>>
    {
    }
}