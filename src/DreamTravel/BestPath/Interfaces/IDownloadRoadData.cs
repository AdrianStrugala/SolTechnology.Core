namespace DreamTravel.BestPath.Interfaces
{
    using System.Collections.Generic;
    using Models;
    using SharedModels;

    public interface IDownloadRoadData
    {
        EvaluationMatrix Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix);
    }
}