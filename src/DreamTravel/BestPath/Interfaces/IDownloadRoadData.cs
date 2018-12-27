namespace DreamTravel.BestPath.Interfaces
{
    using System.Collections.Generic;
    using Models;
    using SharedModels;

    public interface IDownloadRoadData
    {
        EvaluationMatrix Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix);
        List<Path> ExecuteV2(City origin, List<City> destinations);

        EvaluationMatrix ExecuteV3(List<City> listOfCities,
            EvaluationMatrix evaluationMatrix);
    }
}