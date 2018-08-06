using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers
{
    public interface ICalculateBestPath
    {
        Task<List<Path>> Execute(List<City> cities);
    }
}