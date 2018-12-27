namespace DreamTravel.BestPath.Interfaces
{
    using Models;
    using SharedModels;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IDownloadRoadData
    {
        Task<EvaluationMatrix> Execute(List<City> listOfCities,
            EvaluationMatrix evaluationMatrix);
    }
}