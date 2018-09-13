using System.Threading.Tasks;
using DreamTravel.SharedModels;

namespace DreamTravel.LocationOfCity.Interfaces
{
    public interface IFindLocationOfCity
    {
        Task<City> Execute(string cityName);
    }
}