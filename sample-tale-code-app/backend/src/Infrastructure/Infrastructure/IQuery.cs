using System.Threading.Tasks;

namespace DreamTravel.Infrastructure
{
    public interface IQueryHandler<in TQuery, TResult>
    {
        public Task<TResult> Handle(TQuery query);
    }
}
