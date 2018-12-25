using System.Collections.Generic;
using DreamTravel.SharedModels;

namespace DreamTravel.BestPath.Interfaces
{
    public interface ICalculateBestPath
    {
        List<Path> Execute(Command command);
    }
}