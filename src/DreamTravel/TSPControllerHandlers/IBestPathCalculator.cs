using System.Collections.Generic;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using TravelingSalesmanProblem;

namespace DreamTravel.TSPControllerHandlers
{
    public interface IBestPathCalculator
    {
        List<Path> CalculateBestPath(string cities, IProcessInputData processInputData, IProcessOutputData processOutputData, ITSP TSPSolver);
    }
}