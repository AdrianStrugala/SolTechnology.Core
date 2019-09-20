using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.GeolocationData
{
    public interface ICityRepository
    {
        Task<City> GetLocation(string cityName);

        Task<City> GetName(City city);
    }
}