using DreamTravel.SharedModels;

namespace DreamTravel.BestPath.Interfaces
{
    public interface IDownloadCostBetweenTwoCities
    {
        (double, double) Execute(City origin, City destination);
    }
}