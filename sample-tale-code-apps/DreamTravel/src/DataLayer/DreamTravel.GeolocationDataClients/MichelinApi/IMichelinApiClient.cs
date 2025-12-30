using DreamTravel.Domain.Cities;

namespace DreamTravel.GeolocationDataClients.MichelinApi
{
    public interface IMichelinHTTPClient
    {
        Task<(double, double)> DownloadCostBetweenTwoCities(City origin, City destination);
    }
}
