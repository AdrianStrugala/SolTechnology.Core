using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.Features.FindNameOfCity.Interfaces
{
    public interface IFindNameOfCity
    {
        Task<City> Execute(double lat, double lng);
    }
}