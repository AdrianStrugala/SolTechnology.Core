using SolTechnology.Core.CQRS.ResultPattern;

namespace SolTechnology.Core.CQRS
{
    public interface ICommandHandler<in TCommand>
    {
        public Task<Result<Vacuum>> Handle(TCommand command);
    }
}