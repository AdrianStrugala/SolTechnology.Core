using System.Threading.Tasks;

namespace DreamTravel.Domain.Cities
{
    public interface ICityRepository
    {
        Task<City> GetLocation(string cityName);

        Task<City> GetName(City city);
    }
}