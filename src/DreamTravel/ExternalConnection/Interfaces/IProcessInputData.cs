using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection.Interfaces
{
    public interface IProcessInputData
    {
        EvaluationMatrix Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix);
    }
}