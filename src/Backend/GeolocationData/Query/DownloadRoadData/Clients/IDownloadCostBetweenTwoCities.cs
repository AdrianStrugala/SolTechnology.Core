using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.GeolocationData.Query.DownloadRoadData.Clients
{
    public interface IDownloadCostBetweenTwoCities
    {
        Task<(double, double)> Execute(City origin, City destination);
    }
}
