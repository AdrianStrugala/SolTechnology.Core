namespace DreamTravel.BestPath.Interfaces
{
    using Models;
    using SharedModels;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IDownloadRoadData
    {
        EvaluationMatrix Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix);
        List<Path> ExecuteV2(City origin, List<City> destinations);
        
        Task<EvaluationMatrix> ExecuteV4(List<City> listOfCities,
            EvaluationMatrix evaluationMatrix);
    }
}