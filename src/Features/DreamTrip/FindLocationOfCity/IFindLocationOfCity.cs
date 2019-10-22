using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.Features.DreamTrip.FindLocationOfCity
{
    public interface IFindLocationOfCity
    {
        Task<City> Execute(string cityName);
    }
}