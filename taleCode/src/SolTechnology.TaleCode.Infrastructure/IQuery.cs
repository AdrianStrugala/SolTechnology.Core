namespace SolTechnology.TaleCode.Infrastructure
{
    public interface IQuery
    {
    }

    public interface IResult
    {
    }

    public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery where TResult : IResult
    {
        public Task<TResult> Handle(TQuery query);
    }
}