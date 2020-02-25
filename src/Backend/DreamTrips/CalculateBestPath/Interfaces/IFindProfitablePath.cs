using DreamTravel.GeolocationData.Query.DownloadRoadData;

namespace DreamTravel.DreamTrips.CalculateBestPath.Interfaces
{
    public interface IFindProfitablePath
    {
        EvaluationMatrix Execute(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}