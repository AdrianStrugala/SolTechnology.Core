using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection
{
    public interface IProcessOutputData
    {
        List<Path> FormOutputFromTSPResult(List<City> listOfCities, int[] orderOfCities, IEvaluationMatrix evaluationMatrix);
    }
}