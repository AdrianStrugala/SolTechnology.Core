using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.Trips.GeolocationDataClients.MichelinApi
{
    public interface IMichelinApiClient
    {
        Task<(double, double)> DownloadCostBetweenTwoCities(City origin, City destination);
    }
}
