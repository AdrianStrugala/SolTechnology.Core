namespace DreamTravel.BestPath.Interfaces
{
    using Models;
    using SharedModels;
    using System.Collections.Generic;

    public interface IDownloadRoadData
    {
        EvaluationMatrix Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix);
        List<Path> ExecuteV2(City origin, List<City> destinations);

        EvaluationMatrix ExecuteV3(List<City> listOfCities,
            EvaluationMatrix evaluationMatrix);

        EvaluationMatrix ExecuteV4(List<City> listOfCities,
            EvaluationMatrix evaluationMatrix);
    }
}