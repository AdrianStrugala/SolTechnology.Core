using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection
{
    public interface IProcessInputData
    {
        EvaluationMatrix Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix);
    }
}