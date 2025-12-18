using DreamTravel.Domain.Cities;

namespace DreamTravel.GeolocationDataClients.MichelinApi
{
    public interface IMichelinApiClient
    {
        Task<(double, double)> DownloadCostBetweenTwoCities(City origin, City destination);
    }
}
