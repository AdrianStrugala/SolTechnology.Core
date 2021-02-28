using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.DreamTrips.FindNameOfCity
{
    public interface IFindNameOfCity
    {
        Task<City> Handle(FindNameOfCityQuery query);
    }
}