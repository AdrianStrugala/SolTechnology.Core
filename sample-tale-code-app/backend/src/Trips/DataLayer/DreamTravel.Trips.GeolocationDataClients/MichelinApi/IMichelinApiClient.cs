using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.GeolocationData.MichelinApi
{
    public interface IMichelinApiClient
    {
        Task<(double, double)> DownloadCostBetweenTwoCities(City origin, City destination);
    }
}
