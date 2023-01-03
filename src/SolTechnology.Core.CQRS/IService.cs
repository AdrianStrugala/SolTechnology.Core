using SolTechnology.Core.CQRS.ResultPattern;

namespace SolTechnology.Core.CQRS
{
    public interface IService<in TInput, TOutcome>
    {
        public Task<Result<TOutcome>> Execute(TInput input);
    }
}
