using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.DreamTrips.CalculateBestPath.Interfaces
{
    public interface ICalculateBestPath
    {
        Task<CalculateBestPathResult> Execute(List<City> cities);
    }
}