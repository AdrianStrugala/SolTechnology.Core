using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.GeolocationData
{
    public interface IMatrixRepository
    {
        Task<double[]> GetFreeRoadDuration(List<City> cities);

        Task<double[]> GetTollRoadDuration(List<City> cities);

        Task<(double[], double[])> GetCosts(List<City> cities);
    }
}