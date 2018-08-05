using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers
{
    public interface IBestPathCalculator
    {
        Task<List<Path>> Handle(List<City> cities);
    }
}