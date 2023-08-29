using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.GeolocationData
{
    public interface IMichelinApiClient
    {
        Task<(double, double)> DownloadCostBetweenTwoCities(City origin, City destination);
    }
}
