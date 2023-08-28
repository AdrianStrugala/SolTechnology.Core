using System.Threading.Tasks;

namespace DreamTravel.Infrastructure
{
    public interface IService<in TInput, TOutput>
    {
        public Task<TOutput> Execute(TInput command);
    }
}
