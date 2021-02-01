using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.DreamTrips.FindLocationOfCity
{
    public interface IFindLocationOfCity
    {
        Task<City> Handle(FindLocationOfCityQuery query);
    }
}