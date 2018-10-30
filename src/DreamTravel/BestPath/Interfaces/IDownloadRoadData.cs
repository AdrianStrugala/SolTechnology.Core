using System.Collections.Generic;
using DreamTravel.BestPath.Models;
using DreamTravel.SharedModels;

namespace DreamTravel.ExternalConnection.Interfaces
{
    public interface IDownloadRoadData
    {
        EvaluationMatrix Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix);
    }
}