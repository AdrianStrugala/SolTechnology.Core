using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.Features.CalculateBestPath.Interfaces
{
    public interface ICalculateBestPath
    {
        Task<Result> Execute(List<City> cities, bool optimizePath);
    }
}