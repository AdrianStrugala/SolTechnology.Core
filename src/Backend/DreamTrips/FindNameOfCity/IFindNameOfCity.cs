using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.DreamTrips.FindNameOfCity
{
    public interface IFindNameOfCity
    {
        Task<City> Execute(double lat, double lng);
    }
}